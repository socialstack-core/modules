using Api.Contexts;
using Api.Startup;
using Api.Uploader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.DynamicSiteMap
{
	/// <summary>
	/// Serves sitemap files from storage.
	/// </summary>
	[InternalApi]
	public partial class DynamicSiteMapController : AutoController
	{
		private UploadService _uploads;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public DynamicSiteMapController(UploadService uploads)
		{
			_uploads = uploads;
		}

		/// <summary>
		/// Serves the main sitemap.xml or sitemap index.
		/// </summary>
		[HttpGet("sitemap.xml")]
		public async ValueTask Sitemap(HttpContext httpContext, Context context)
		{
			await ServeSitemapFile(httpContext, context, "sitemap.xml");
		}

		/// <summary>
		/// Serves a sub-sitemap file (sitemap-1.xml, sitemap-2.xml, etc.).
		/// </summary>
		[HttpGet("sitemap/{id}")]
		public async ValueTask SitemapSub(HttpContext httpContext,Context context, [FromRoute] uint id)
		{
			await ServeSitemapFile(httpContext, context, $"sitemap-{id}.xml");
		}

		private async ValueTask ServeSitemapFile(HttpContext httpContext, Context context, string fileName)
		{
			var stream = await _uploads.GetFileStreamForStoragePath($"sitemap/1-{fileName}", false);

			if (stream == null)
			{
				httpContext.Response.StatusCode = 404;
				return;
			}

			httpContext.Response.ContentType = "application/xml";

			await stream.CopyToAsync(httpContext.Response.Body);
			await httpContext.Response.Body.FlushAsync();
			stream.Dispose();
		}
	}
}
