using Newtonsoft.Json;
using Api.Configuration;
using Api.Database;
using Api.Signatures;
using Api.Startup;
using System;
using System.Web;
using Api.Users;
using Api.SocketServerLibrary;

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
		/// If this is an image, the original width.
		/// </summary>
		public int? Width;

		/// <summary>
		/// If this is an image, the original height.
		/// </summary>
		public int? Height;
		
		/// <summary>
		/// True if this upload is an image.
		/// </summary>
		public bool IsImage;

		/// <summary>
		/// True if this is a private upload and requires a signature in order to access it publicly.
		/// </summary>
		public bool IsPrivate;
		
		/// <summary>
		/// Working memory only temporary filesystem path. Can be null if something has already relocated the upload and it is "done".
		/// </summary>
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
				if(IsPrivate){
					if (_sigService == null)
					{
						_sigService = Services.Get<SignatureService>();
					}
					
					var writer = Writer.GetPooled();
					writer.Start(null);
					writer.WriteASCII("private:");
					writer.WriteS(Id);
					writer.Write((byte)'.');
					if (FileType != null)
					{
						writer.WriteASCII(FileType);
					}
					writer.Write(TimestampStart, 0, 3);
					writer.WriteS(DateTime.UtcNow.Ticks);
					writer.Write(SignatureStart, 0, 3);
					_sigService.SignHmac256AlphaChar(writer);
					var result = writer.ToASCIIString();
					writer.Release();
					return result;
				}
				
				return "public:" + Id + "." + FileType;
			}
		}

		/// <summary>
		/// File path to this content using the given size name.
		/// It's either "original" or a specific width in pixels, e.g. "400".
		/// </summary>
		public string GetFilePath(string sizeName, bool omitExt = false)
        {
            return AppSettings.Configuration[IsPrivate ? "ContentPrivate" : "Content"] + GetRelativePath(sizeName, omitExt);
        }

		/// <summary>
		/// Relative path used as both the actual filepath and the URL
		/// </summary>
		/// <param name="sizeName"></param>
        /// <param name="omitExt"></param>
		/// <returns></returns>
		public string GetRelativePath(string sizeName, bool omitExt = false)
        {
            if (omitExt)
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
		/// Gets the file path of this ref.
		/// </summary>
		/// <returns></returns>
		public string GetFilePath(string sizeName, string altExtension = null)
		{
			return AppSettings.Configuration[Scheme == "private" ? "ContentPrivate" : "Content"] + GetRelativePath(sizeName, false, altExtension);
		}

		/// <summary>
		/// Relative path of this ref.
		/// </summary>
		/// <param name="sizeName"></param>
		/// <param name="omitExt"></param>
		/// <returns></returns>
		public string GetRelativePath(string sizeName, bool omitExt = false, string altExtension = null)
		{
			if (altExtension != null)
			{
				return File + "-" + sizeName + "." + altExtension;
			}
			else if (omitExt)
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
