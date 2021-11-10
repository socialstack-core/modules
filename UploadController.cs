using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using System.IO;
using Microsoft.Extensions.Primitives;
using Api.Startup;
using System;

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

			var fileName = body.File.FileName;

			if (string.IsNullOrEmpty(fileName) || fileName.IndexOf('.') == -1)
			{
				throw new PublicException("Content-Name header should be a filename with the type.", "invalid_name");
			}
			
			// Write to a temporary path first:
			var tempFile = System.IO.Path.GetTempFileName();

			// Save the content now:
			var fileStream = new FileStream(tempFile, FileMode.OpenOrCreate);

			await body.File.CopyToAsync(fileStream);
			fileStream.Close();

			// Upload the file:
			var upload = await (_service as UploadService).Create(
				context,
				fileName,
				tempFile,
				null,
				body.IsPrivate
			);

			if (upload == null)
			{
				// It failed. Usually because white/blacklisted.
				Response.StatusCode = 401;
				return;
			}

			await OutputJson(context, upload, "*");
		}
		
		/// <summary>
		/// Upload a file with efficient support for huge ones.
		/// </summary>
		/// <returns></returns>
		[HttpPut("create")]
		public async ValueTask Upload()
		{
			if (!Request.Headers.TryGetValue("Content-Name", out StringValues name))
			{
				throw new PublicException("Content-Name header is required", "no_name");
			}

			var fileName = name.ToString();

			if (string.IsNullOrEmpty(fileName) || fileName.IndexOf('.') == -1)
			{
				throw new PublicException("Content-Name header should be a filename with the type.", "invalid_name");
			}

			var isPrivate = false;

			if (Request.Headers.TryGetValue("Private-Upload", out StringValues privateState))
			{
				var privState = privateState.ToString().ToLower().Trim();

				isPrivate = privState == "true" || privState == "1" || privState == "yes";
			}
			
			var context = await Request.GetContext();
			
			// The stream for the actual file is just the entire body:
			var contentStream = Request.Body;

			var tempFile = System.IO.Path.GetTempFileName();

			var fileStream = new FileStream(tempFile, System.IO.FileMode.OpenOrCreate);
			await contentStream.CopyToAsync(fileStream);
			fileStream.Close();
			
			// Create the upload entry for it:
			var upload = await (_service as UploadService).Create(
				context,
				fileName,
				tempFile,
				null,
				isPrivate
			);

			if (upload == null)
			{
				// It failed for some generic reason.
				Response.StatusCode = 401;
				return;
			}

			await OutputJson(context, upload, "*");
		}

		/// <summary>
		/// Uploads a transcoded file. The body of the client request is expected to be a tar of the files, using a directory called "output" at its root.
		/// </summary>
		/// <returns></returns>
		[HttpPut("transcoded/{id}")]
		public async ValueTask TranscodedTar([FromRoute] uint id, [FromQuery] string token)
		{
			if (!(_service as UploadService).IsValidTranscodeToken(id, token))
			{
				throw new PublicException("Invalid transcode token", "tx_token_bad");
			}

			var context = await Request.GetContext();

			// Proceed only if the target doesn't already exist.
			// Then there must be a GET arg called sig containing an alphachar HMAC of recent time-id. It expires in 24h.

			// The stream for the actual file is just the entire body:
			var contentStream = Request.Body;

			// Expect a tar:
			await (_service as UploadService).ExtractTarToStorage(context, id, "chunks", contentStream);
		}
	}

}
