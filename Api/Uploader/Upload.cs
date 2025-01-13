using Newtonsoft.Json;
using Api.Database;
using Api.Signatures;
using Api.Startup;
using System;
using Api.Users;
using Api.SocketServerLibrary;
using System.Threading.Tasks;
using Api.Translate;
using Api.AutoForms;

namespace Api.Uploader
{
    /// <summary>
    /// Meta for uploaded files.
    /// </summary>
    [ListAs("Uploads", Explicit = true)]
    public partial class Upload : VersionedContent<uint>
    {
        /// <summary>
        /// The signature service for priv uploads.
        /// </summary>
        private static UploadService _uploadService;

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
        /// The alternative name for the image
        /// </summary>
        [Data("hint", "The alternative name/title to display for the image")]
        public string Alt;

        /// <summary>
        /// The author/photographer for the image
        /// </summary>
        [Data("hint", "The author/photographer of the content")]
        public string Author;

        /// <summary>
        /// The number of times the image is used
        /// </summary>
        [Data("hint", "The number of times the image is used")]
        public int? UsageCount;

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

        /// <summary>
        /// Gets a ref which may be signed.
        /// The HMAC is for the complete string "private:ID.FILETYPE?t=TIMESTAMP&amp;s="
        /// </summary>
        public string Ref
        {
            get
            {
                if (_uploadService == null)
                {
                    _uploadService = Services.Get<UploadService>();
                }

                return _uploadService.BuildRef(this);
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

            if (string.IsNullOrEmpty(fileType))
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
                return (string.IsNullOrEmpty(Subdirectory) ? "" : Subdirectory + '/') + Id + "-" + sizeName;
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
                return new FileRef()
                {
                };
            }

            var protoIndex = refText.IndexOf(':');
            var scheme = (protoIndex == -1) ? "https" : refText.Substring(0, protoIndex);

            refText = protoIndex == -1 ? refText : refText.Substring(protoIndex + 1);
            var basePath = refText;

			var argsIndex = basePath.IndexOf('?');
			var queryStr = "";
			if (argsIndex != -1)
			{
				queryStr = basePath.Substring(argsIndex + 1);
				basePath = basePath.Substring(0, argsIndex);
			}

			string fileParts;
            string fileType = null;

            var lastSlash = basePath.LastIndexOf('/');
            var dotIndex = basePath.IndexOf('.', lastSlash);

            if (dotIndex != -1)
            {
				// It has a filetype - might have variants too.

				var multiTypes = basePath.IndexOf('|', dotIndex);
				if (multiTypes != -1)
				{
					// Remove multi types from basepath:
					basePath = basePath.Substring(0, multiTypes);
				}

				fileType = basePath.Substring(dotIndex + 1);
                fileParts = basePath.Substring(0, dotIndex);
            }
            else
            {
                fileParts = basePath;
            }

            return new FileRef()
            {
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
