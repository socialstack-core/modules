using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;

namespace Api.Uploader
{
    /// <summary>
    /// Handles file upload endpoints.
    /// </summary>

    [Route("v1/upload")]
	public partial class UploadController : AutoController<Upload>
    {
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UploadController() : base()
        {
        }
		
		/// <summary>
		/// Upload a file.
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
		[HttpPost("create")]
		public async ValueTask Upload([FromForm] FileUploadBody body)
		{
			var context = await Request.GetContext();

			// body = await Events.Upload.Create.Dispatch(context, body, Response) as FileUploadBody;

			// Upload the file:
			var upload = await (_service as UploadService).Create(
				context,
				body.File
			);

			if (upload == null)
			{
				// It failed. Usually because white/blacklisted.
				Response.StatusCode = 401;
				return;
			}

			await OutputJson(context, upload, "*");
		}

    }

}
