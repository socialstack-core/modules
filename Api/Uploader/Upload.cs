using Newtonsoft.Json;
using Api.Database;
using Api.Signatures;
using Api.Startup;
using System;
using Api.Users;
using Api.SocketServerLibrary;
using System.Threading.Tasks;


namespace Api.Uploader
{
	/// <summary>
	/// Meta for uploaded files.
	/// </summary>
	[ListAs("Uploads", Explicit=true)]
    public partial class Upload : VersionedContent<uint>
	{
		/// <summary>
		/// The signature service for priv uploads.
		/// </summary>
		private static SignatureService _sigService;

		/// <summary>
		/// The original file name.
		/// </summary>
		[DatabaseField(Length = 300)]
		[Meta("title")]
		public string OriginalName;

		/// <summary>
		/// The lowercased file type, e.g. "png".
		/// </summary>
		[DatabaseField(Length = 15)]
		public string FileType;

		/// <summary>
		/// filetype variants separated by | if there are any.
		/// </summary>
		[DatabaseField(Length = 64)]
		public string Variants;

		/// <summary>
		/// A blurhash if there is one.
		/// </summary>
		[DatabaseField(Length = 100)]
		public string Blurhash;

		/// <summary>
		/// If this is an image, the original width.
		/// </summary>
		public int? Width;

		/// <summary>
		/// If this is an image, the original height.
		/// </summary>
		public int? Height;

		/// <summary>
		/// If this is an image, the horizontal focal point (as a percentage).
		/// </summary>
		public int? FocalX;

		/// <summary>
		/// If this is an image, the vertical focal point (as a percentage).
		/// </summary>
		public int? FocalY;

		/// <summary>
		/// True if this upload is an image.
		/// </summary>
		public bool IsImage;

		/// <summary>
		/// True if this is a private upload and requires a signature in order to access it publicly.
		/// </summary>
		public bool IsPrivate;

		/// <summary>True if this is a video.</summary>
		public bool IsVideo;

		/// <summary>True if this is audio.</summary>
		public bool IsAudio;

		/// <summary>The transcode state. 2 means it's been transcoded, 1 is transcode in progress.</summary>
		public int TranscodeState;

		/// <summary>
		/// The subdirectory that this upload was put into, if any. Ensure that users can't directly set this.
		/// </summary>
		[JsonIgnore]
		public string Subdirectory;

		/// <summary>
		/// Working memory only temporary filesystem path. Can be null if something has already relocated the upload and it is "done".
		/// </summary>
		[JsonIgnore]
		public string TemporaryPath { get; set; }
		
		private static byte[] TimestampStart = new byte[]{(byte)'?', (byte)'t', (byte)'='};
		private static byte[] SignatureStart = new byte[]{(byte)'&', (byte)'s', (byte)'='};
		
		/// <summary>
		/// Gets a ref which may be signed.
		/// The HMAC is for the complete string "private:ID.FILETYPE?t=TIMESTAMP&amp;s="
		/// </summary>
		public string Ref
		{
			get{

				var writer = Writer.GetPooled();
				writer.Start(null);

				if (IsPrivate)
				{
					if (_sigService == null)
					{
						_sigService = Services.Get<SignatureService>();
					}

					writer.WriteASCII("private:");

					if (!string.IsNullOrEmpty(Subdirectory))
					{
						writer.WriteASCII(Subdirectory);
						writer.Write((byte)'/');
					}

					writer.WriteS(Id);
					writer.Write((byte)'.');
					if (FileType != null)
					{
						writer.WriteASCII(FileType);
					}
					// Private uploads don't support variants (yet) because the signature would include them
					writer.Write(TimestampStart, 0, 3);
					writer.WriteS(DateTime.UtcNow.Ticks);
					writer.Write(SignatureStart, 0, 3);
					_sigService.SignHmac256AlphaChar(writer);
				}
				else
				{
					writer.WriteASCII("public:");

					if (!string.IsNullOrEmpty(Subdirectory))
					{
						writer.WriteASCII(Subdirectory);
						writer.Write((byte)'/');
					}

					writer.WriteS(Id);
					writer.Write((byte)'.');
					if (FileType != null)
					{
						writer.WriteASCII(FileType);
					}
					if (Variants != null)
					{
						writer.Write((byte)'|');
						writer.WriteASCII(Variants);
					}
				}

				if (IsImage && Width.HasValue && Height.HasValue)
				{
					writer.WriteASCII(IsPrivate ? "&w=" : "?w=");
					writer.WriteS(Width.Value);
					writer.WriteASCII("&h=");
					writer.WriteS(Height.Value);

					if (!string.IsNullOrEmpty(Blurhash))
					{
						writer.WriteASCII("&b=");
						writer.WriteASCII(System.Uri.EscapeDataString(Blurhash));
					}

					if (FocalX.HasValue && FocalY.HasValue)
                    {
						writer.WriteASCII("&fx=");
						writer.WriteS(FocalX.Value);
						writer.WriteASCII("&fy=");
						writer.WriteS(FocalY.Value);
					}

				}

				var result = writer.ToASCIIString();
				writer.Release();
				return result;
			}
		}

		/// <summary>
		/// Read the bytes of the given variant of this upload. Convenience method for the method of the same name on UploadService.
		/// </summary>
		/// <param name="variant"></param>
		/// <returns></returns>
		public async ValueTask<byte[]> ReadFile(string variant = "original")
		{
			return await Services.Get<UploadService>().ReadFile(this, variant);
		}

		/// <summary>
		/// Gets an appropriate mime type, when possible, based on the file type.
		/// </summary>
		public string GetMimeType(string variant = "original")
		{
			var fileType = FileType;

			if (variant != null)
			{
				var lastDot = variant.LastIndexOf('.');

				if (lastDot != -1)
				{
					// This is actually a transcoded file and is of a different type.
					fileType = variant.Substring(lastDot + 1);
				}
			}

			if(string.IsNullOrEmpty(fileType))
			{
				return "application/octet-stream";
			}
			
			return MimeTypeMap.GetMimeType(fileType);
		}

		/// <summary>
		/// File path to this content using the given size name.
		/// It's either "original" or a specific width in pixels, e.g. "400".
		/// </summary>
		public string GetFilePath(string sizeName, bool omitExt = false)
        {
			string basePath;

			if (IsPrivate)
			{
				basePath = "Content/content-private/";
			}
			else
			{
				basePath = "Content/content/";
			}

			return basePath + GetRelativePath(sizeName, omitExt);
        }

		/// <summary>
		/// Relative path used as both the actual filepath and the URL
		/// </summary>
		/// <param name="sizeName"></param>
        /// <param name="omitExt"></param>
		/// <returns></returns>
		public string GetRelativePath(string sizeName, bool omitExt = false)
        {
            if (omitExt || sizeName.IndexOf('.') != -1)
            {
				return (string.IsNullOrEmpty(Subdirectory) ? "" : Subdirectory + '/') +  Id + "-" + sizeName;
            }
            else
            {
                return (string.IsNullOrEmpty(Subdirectory) ? "" : Subdirectory + '/') + Id + "-" + sizeName + "." + FileType;
            }
        }

		/// <summary>
		/// The filename, excluding any subdirectory.
		/// </summary>
		/// <param name="sizeName"></param>
		/// <param name="omitExt"></param>
		/// <returns></returns>
		public string GetStoredFilename(string sizeName, bool omitExt = false)
		{
			if (omitExt || sizeName.IndexOf('.') != -1)
			{
				return Id + "-" + sizeName;
			}
			else
			{
				return Id + "-" + sizeName + "." + FileType;
			}
		}
    }

	/// <summary>
	/// A parsed fileref.
	/// </summary>
	public struct FileRef
	{

		/// <summary>
		/// Parses general meta out of a textual ref.
		/// </summary>
		/// <param name="refText"></param>
		/// <returns></returns>
		public static FileRef Parse(string refText)
		{
			if (string.IsNullOrEmpty(refText))
			{
				return new FileRef() {
				};
			}

			var protoIndex = refText.IndexOf(':');
			var scheme = (protoIndex == -1) ? "https" : refText.Substring(0, protoIndex);
			
			refText = protoIndex == -1 ? refText : refText.Substring(protoIndex + 1);
			string fileParts;
			string fileType = null;

			var lastIndexOf = refText.LastIndexOf('.');

			if (lastIndexOf != -1)
			{
				fileType = refText.Substring(lastIndexOf + 1);
				fileParts = refText.Substring(0, lastIndexOf);
			}
			else
			{
				fileParts = refText;
			}

			return new FileRef(){
				Scheme = scheme,
				FileType = fileType,
				File = fileParts
			};
		}

		/// <summary>
		/// The ref's scheme.
		/// </summary>
		public string Scheme;

		/// <summary>
		/// The filetype of the ref, if there is one.
		/// </summary>
		public string FileType;

		/// <summary>
		/// The filename parts. Required. Excludes type.
		/// </summary>
		public string File;

		/// <summary>
		/// Outputs the textual ref.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Scheme + ':' + File + '.' + FileType;
		}

		/// <summary>
		/// Gets the file path of this ref. Null if it is not a file ref.
		/// </summary>
		/// <returns></returns>
		public string GetFilePath(string sizeName)
		{
			string basePath;

			if (Scheme == "private")
			{
				basePath = "Content/content-private/";
			}
			else if (Scheme == "public")
			{
				basePath = "Content/content/";
			}
			else
			{
				return null;
			}

			return basePath + GetRelativePath(sizeName, false);
		}

		/// <summary>
		/// Relative path of this ref.
		/// </summary>
		/// <param name="sizeName"></param>
		/// <param name="omitExt"></param>
		/// <returns></returns>
		public string GetRelativePath(string sizeName, bool omitExt = false)
		{
			if (omitExt || sizeName.IndexOf('.') != -1)
			{
				return File + "-" + sizeName;
			}
			else
			{
				return File + "-" + sizeName + "." + FileType;
			}
		}
	}

}
