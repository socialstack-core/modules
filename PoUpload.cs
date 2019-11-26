using Microsoft.AspNetCore.Http;


namespace Api.Translate
{
    /// <summary>
    /// Used when uploading .po files
    /// </summary>
    public class PoUpload
    {
		/// <summary>
		/// The file being uploaded.
		/// </summary>
        public IFormFile File { get; set; }
    }
}
