using ImageMagick;
using Org.BouncyCastle.Asn1.X500;

namespace Api.Uploader
{
    /// <summary>
    /// List of changes when replacing media refs 
    /// </summary>
    public class MediaRef
    {
        /// <summary>
        /// The file type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The ID of the upload.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// The name of the ref
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the ref
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Name of the underlying field this ref is in, if any
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// The target URL
        /// </summary>
        public string Url => $"/en-admin/{Type.ToLower()}/{Id.ToString()}";

        /// <summary>
        /// The existing ref which is being replaced/updated
        /// </summary>
        public string ExistingRef { get; set; }

        /// <summary>
        /// The new ref
        /// </summary>
        public string UpdatedRef { get; set; }

        /// <summary>
        /// The status of potential/actiual update
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The locale being used 
        /// </summary>
        public uint LocaleId { get; set; }
    }
}
