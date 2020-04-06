using Newtonsoft.Json;
using Api.Configuration;
using Api.Database;
using Api.Signatures;
using Api.Startup;
using System;
using System.Web;
using Api.Users;

namespace Api.Uploader
{
	/// <summary>
	/// Meta for uploaded files.
	/// </summary>
    public partial class Upload : RevisionRow
	{
		/// <summary>
		/// The signature service for priv uploads.
		/// </summary>
		private static ISignatureService _sigService;

		/// <summary>
		/// The original file name.
		/// </summary>
		[DatabaseField(Length = 300)]
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
		/// The upload file ref is of either of these forms:
		/// Private files: private:{optionalServerDomainName/}Id.fileType
		/// Public files: public:{optionalServerDomainName/}Id.fileType
		/// E.g. public:4.jpg or public:node14.cdn.site.com/4.jpg.
		/// From this a full public URL can be constructed. If the server domain name is omitted, 
		/// it is simply "this" one.
		/// To view a private file you must obtain a signed URL.
		/// </summary>
		public string Ref
		{
			get
			{
				return (IsPrivate ? "private:" : "public:") + Id + "." + FileType;
			}
		}
		
		/// <summary>
		/// The public url of this content (including the domain from the site configuration).
		/// </summary>
		public string GetPublicUrl(string sizeName, bool omitExt = false)
        {
			var path =  "/content" + (IsPrivate ? "-private/" : "/") + GetRelativePath(sizeName, omitExt);
			var url = AppSettings.Configuration["PublicUrl"] + path;

			if (IsPrivate){
				var timestamp = GetUnixTimestamp();

				if (_sigService == null)
				{
					_sigService = Services.Get<ISignatureService>();
				}

				var sig = _sigService.Sign(path, timestamp);

				url += "?s=" + HttpUtility.UrlEncode(sig) + "&t=" + timestamp;
			}

			return url;
        }

		/// <summary>
		/// Unix epoch
		/// </summary>
		private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);

		/// <summary>
		/// Current unix timestamp as a long
		/// </summary>
		/// <returns></returns>
		public long GetUnixTimestamp()
		{
			return (long)(DateTime.UtcNow.Subtract(_unixEpoch)).TotalSeconds;
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
