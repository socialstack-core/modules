using Microsoft.AspNetCore.Http;


namespace Api.Uploader
{
    /// <summary>
    /// The post body when uploading a file.
    /// </summary>
    public partial class FileUploadBody
    {
		/// <summary>
		/// The file being uploaded.
		/// </summary>
        public IFormFile File { get; set; }
    }
}
