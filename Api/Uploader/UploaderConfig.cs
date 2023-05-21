using Api.Configuration;

namespace Api.Uploader
{
    /// <summary>
    /// Configuration settings relating to file uploads and image processing.
    /// </summary>
    public partial class UploaderConfig : Config
    {
		/// <summary>
		/// When an image is uploaded, it'll be automatically resized to each of these sizes.
		/// </summary>
		[Frontend]
		public int[] ImageSizes { get; set; } = new int[]{ 32, 64, 100, 128, 200, 256, 512, 768, 1024, 1920, 2048 };

        /// <summary>
        /// When an image is uploaded, it'll be automatically resized to the exact height and width defined in this list.
		/// This isn't necessarily the same aspect ratio as the original image, so cropping can occur.
		/// It is recommended to set a focal point prior to resizing, as this will be used to inform how the cropped image is produced.
		/// Note that sizes are defined in [height]x[width] format (e.g. "177x118"), with an optional zoom level (e.g. "1024x768@3x")
        /// </summary>
        public string[] ImageCrops { get; set; } = new string[] {};

		/// <summary>
		/// True if image uploads should be processed.
		/// </summary>
		public bool ProcessImages { get; set; } = true;

		/// <summary>
		/// True if all image uploads should be transcoded to webp, unless they were webp already.
		/// If it's already a web friendly format like png or jpg, the original will be resized as well.
		/// </summary>
		public bool TranscodeToWebP { get; set; } = true;

        /// <summary>
        /// Config for webp transcoding.
        /// </summary>
        public WebpConfig WebPConfig { get; set; }

        /// <summary>
        /// When an image is uploaded, this will generate a blurhash for it which gets stored in the upload data and the ref.
        /// </summary>
        public bool GenerateBlurhash { get; set; } = false;

		/// <summary>
		/// Uploader subdirectory (optional)
		/// </summary>
		public string Subdirectory { get; set; }
		
		/// <summary>
		/// Track ref usage.
		/// </summary>
		public bool TrackRefUsage { get; set; } = false;
    }
	
}
