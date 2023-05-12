using Api.Configuration;

namespace Api.Uploader
{
	/// <summary>
	/// The appsettings.json config block for email config.
	/// </summary>
    public partial class UploaderConfig : Config
    {
		/// <summary>
		/// When an image is uploaded, it'll be automatically resized to each of these sizes.
		/// </summary>
		public int[] ImageSizes = new int[]{ 32, 64, 100, 128, 200, 256, 512, 1024, 2048 };

        /// <summary>
        /// When an image is uploaded, it'll be automatically resized to the exact height and width defined in this list.
		/// This isn't necessarily the same aspect ratio as the original image, so cropping can occur.
		/// Note that sizes are defined in [height]x[width] format (e.g. "177x118"), with an optional zoom level (e.g. "1024x768@3x")
        /// </summary>
        public string[] ImageCrops = new string[] {};

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
		/// When an image is uploaded, this will generate a blurhash for it which gets stored in the upload data and the ref.
		/// </summary>
		public bool GenerateBlurhash { get; set; } = false;

		/// <summary>
		/// Uploader subdirectory (optional)
		/// </summary>
		public string Subdirectory { get; set; }
    }
	
}
