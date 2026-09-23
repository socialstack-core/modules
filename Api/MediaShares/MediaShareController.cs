using Api.Contexts;
using Api.Startup;
using Api.Uploader;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Api.MediaShares
{
    /// <summary>Handles mediaShare endpoints.</summary>
    [Route("v1/mediaShare")]
	public partial class MediaShareController : AutoController<MediaShare>
    {
		private readonly MediaShareService _mediaShares;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public MediaShareController(MediaShareService mediaShares) : base()
		{
			_mediaShares = mediaShares;
		}

		/// <summary>
		/// GET /v1/mediaShare/zip/{slug}
		/// Streams a zip containing every file in the given share. Download count is incremented by one.
		/// </summary>
		[HttpGet("zip/{slug}")]
		public async ValueTask Zip(Context context, HttpResponse response, [FromRoute] string slug)
		{
			var share = await _mediaShares.Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug).First(context);

			if (share == null || MediaShareService.IsExpired(share))
			{
				response.StatusCode = 404;
				return;
			}

			var ids = share.Mappings.Get("uploads");

			if (ids == null || ids.Count == 0)
			{
				response.StatusCode = 404;
				return;
			}

			var uploadService = Services.Get<UploadService>();
			var uintIds = ids.Select(id => (uint)id).ToList();
			var uploads = await uploadService.Where("Id=[?]", DataOptions.IgnorePermissions).Bind(uintIds).ListAll(context);
			var byId = uploads.ToDictionary(upload => upload.Id);

			response.ContentType = "application/zip";
			response.Headers.ContentDisposition = "attachment; filename=share-" + slug + ".zip";

			var usedNames = new HashSet<string>();

			using (var zipStream = new ZipOutputStream(response.Body))
			{
				zipStream.SetLevel(3);

				foreach (var id in ids)
				{
					if (!byId.TryGetValue((uint)id, out Upload upload))
					{
						continue;
					}

					byte[] bytes;

					try
					{
						bytes = await uploadService.ReadFile(upload);
					}
					catch
					{
						continue;
					}

					if (bytes == null)
					{
						continue;
					}

					var entryName = GetUniqueName(upload.OriginalName, upload.Id, usedNames);
					zipStream.PutNextEntry(new ZipEntry(entryName));
					zipStream.Write(bytes, 0, bytes.Length);
					zipStream.CloseEntry();
				}
			}

			await _mediaShares.CountDownload(context, share);
		}

		/// <summary>
		/// GET /v1/mediaShare/file/{slug}/{uploadId}
		/// Streams a single file out of a share, as-is and without a zip wrapper. Download count is incremented by one.
		/// The upload must belong to the share.
		/// </summary>
		[HttpGet("file/{slug}/{uploadId}")]
		public async ValueTask File(Context context, HttpResponse response, [FromRoute] string slug, [FromRoute] uint uploadId)
		{
			var share = await _mediaShares.Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug).First(context);

			if (share == null || MediaShareService.IsExpired(share))
			{
				response.StatusCode = 404;
				return;
			}

			var ids = share.Mappings.Get("uploads");

			if (ids == null || !ids.Any(id => (uint)id == uploadId))
			{
				response.StatusCode = 404;
				return;
			}

			var uploadService = Services.Get<UploadService>();
			var upload = await uploadService.Get(context, uploadId, DataOptions.IgnorePermissions);

			if (upload == null)
			{
				response.StatusCode = 404;
				return;
			}

			var bytes = await uploadService.ReadFile(upload);

			if (bytes == null)
			{
				response.StatusCode = 404;
				return;
			}

			await _mediaShares.CountDownload(context, share);

			response.ContentType = upload.GetMimeType();
			response.Headers.ContentDisposition = "inline; filename=" + upload.OriginalName;
			await response.Body.WriteAsync(bytes, 0, bytes.Length);
		}

		private static string GetUniqueName(string originalName, uint uploadId, HashSet<string> usedNames)
		{
			var baseName = string.IsNullOrEmpty(originalName) ? "file-" + uploadId : originalName;
			var extension = Path.GetExtension(baseName);
			var nameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
			var result = baseName;
			var counter = 1;

			while (usedNames.Contains(result))
			{
				result = nameWithoutExt + " (" + counter++ + ")" + extension;
			}

			usedNames.Add(result);
			return result;
		}
    }
}