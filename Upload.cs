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
	[ListAs("Uploads")]
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

}
