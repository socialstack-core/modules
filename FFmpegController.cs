using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Api.Uploader;
using Microsoft.AspNetCore.Mvc;


namespace Api.FFmpeg
{
	/// <summary>
	/// Handles an endpoint which describes the permissions on each role.
	/// </summary>

	[Route("v1/ffmpeghelper")]
	[ApiController]
	public partial class FFMpegController : ControllerBase
	{
		private FFmpegService _ffmpegService;
		private UploadService _uploadService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FFMpegController(
			FFmpegService svc, UploadService uploads
		)
		{
			_ffmpegService = svc;
			_uploadService = uploads;
		}

		/// <summary>
		/// Requests to transcode a particular video.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("transcode/{id}")]
		public async Task<object> Transcode([FromRoute] uint id)
		{
			var ctx = Request.GetContext();

			if (ctx.RoleId != 1 && ctx.RoleId != 2)
			{
				return null;
			}

			var upload = await _uploadService.Get(ctx, id, DataOptions.IgnorePermissions);

			if (upload == null)
			{
				return null;
			}

			if (_ffmpegService.Transcode(ctx, upload))
			{
				return new
				{
					success = true
				};
			}

			return null;
		}
	}
}
