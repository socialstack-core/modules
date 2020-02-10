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
	[ApiController]
	public partial class UploadController : ControllerBase
    {
        private IUploadService _uploads;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UploadController(
			IUploadService Uploads
        )
        {
			_uploads = Uploads;
        }

		/// <summary>
		/// GET /v1/upload/2/
		/// Returns the upload data for a single item.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Upload> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _uploads.Get(context, id);
			return await Events.UploadLoad.Dispatch(context, result, Response);
		}
		
		/// <summary>
		/// DELETE /v1/upload/2/
		/// Deletes an upload
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Upload> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _uploads.Get(context, id);
			result = await Events.UploadDelete.Dispatch(context, result, Response);

			if (result == null || !await _uploads.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return result;
		}

		/// <summary>
		/// GET /v1/upload/list
		/// Lists all uploads available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Upload>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/upload/list
		/// Lists filtered uploads available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Upload>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Upload>(filters);

			filter = await Events.UploadList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _uploads.List(context, filter);
			return new Set<Upload>() { Results = results };
		}

		/*
		/// <summary>
		/// POST /v1/upload/
		/// Creates a new upload. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Upload> Create([FromBody] UploadAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var upload = new Upload
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, upload))
			{
				return null;
			}

			form = await Events.UploadCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			upload = await _uploads.Create(context, form.Result);

			if (upload == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return upload;
        }
		*/

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

			body = await Events.UploadCreate.Dispatch(context, body, Response);

			// Upload the file:
			var upload = await _uploads.Create(
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

		/// <summary>
		/// POST /v1/upload/1/
		/// Updates a nav menu item with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Upload> Update([FromRoute] int id, [FromBody] UploadAutoForm form)
		{
			var context = Request.GetContext();

			var upload = await _uploads.Get(context, id);
			
			if (!ModelState.Setup(form, upload)) {
				return null;
			}

			form = await Events.UploadUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			upload = await _uploads.Update(context, form.Result);

			if (upload == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return upload;
		}

    }

}
