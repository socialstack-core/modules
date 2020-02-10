using Newtonsoft.Json;
using Api.Configuration;
using Api.Database;


namespace Api.Uploader
{
	/// <summary>
	/// Meta for uploaded files.
	/// </summary>
    public partial class Upload : DatabaseRow
	{
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
		public string GetPublicUrl(string sizeName)
        {
			return AppSettings.Configuration["PublicUrl"] + "/content/" + GetRelativePath(sizeName);
        }

        /// <summary>
        /// File path to this content using the given size name.
		/// It's either "original" or a specific width in pixels, e.g. "400".
        /// </summary>
        public string GetFilePath(string sizeName)
        {
            return AppSettings.Configuration[IsPrivate ? "ContentPrivate" : "Content"] + GetRelativePath(sizeName);
        }

		/// <summary>
		/// Relative path used as both the actual filepath and the URL
		/// </summary>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public string GetRelativePath(string sizeName)
        {
            return (IsPrivate ? "private/" : "") + Id + "-" + sizeName + "." + FileType;
        }
    }

}
