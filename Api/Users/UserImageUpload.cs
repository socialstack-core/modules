using Microsoft.AspNetCore.Http;


namespace Api.Users
{
    /// <summary>
    /// Used when uploading user files
    /// </summary>
    public class UserImageUpload
    {
		/// <summary>
		/// The type of upload this is. avatar etc. Just the field name without 'ref' lowercased.
		/// </summary>
        public string Type { get; set; }
        
		/// <summary>
		/// The file being uploaded.
		/// </summary>
        public IFormFile File { get; set; }
    }
}
