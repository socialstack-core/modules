using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Uploader
{
    /// <summary>
    /// Handles file upload endpoints.
    /// </summary>

    [Route("v1/uploader")]
	public partial class UploadController : AutoController<Upload>
    {
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UploadController() : base()
        {
        }
		
		/// <summary>
		/// Upload a file for this user.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		[HttpPost("upload")]
		public async Task<object> Upload([FromRoute] int id, [FromForm] FileUploadBody body)
		{
			var context = Request.GetContext();

			// body = await Events.Upload.Create.Dispatch(context, body, Response) as FileUploadBody;

			// Upload the file:
			var upload = await (_service as IUploadService).Create(
				context,
				body.File
			);

			if (upload == null)
			{
				// It failed.
				return null;
			}
			
			return new
			{
				id = upload.Id,
				uploadRef = upload.Ref,
				publicUrl = upload.GetPublicUrl("original"),
				isImage = upload.IsImage
			};
		}

    }

}
