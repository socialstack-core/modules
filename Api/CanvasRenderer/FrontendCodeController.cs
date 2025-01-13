using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;
using System.Text;
using Microsoft.Extensions.Primitives;
using Api.SocketServerLibrary;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles requests to /pack/* for frontend code files.
	/// </summary>

	public partial class FrontendCodeController : Controller
    {
		private FrontendCodeService _codeService;

		/// <summary>
		/// Instanced automatically per request.
		/// </summary>
		/// <param name="codeService"></param>
		public FrontendCodeController(FrontendCodeService codeService)
		{
			_codeService = codeService;
		}

		/// <summary>
		/// Reloads a prebuilt UI
		/// </summary>
		[Route("/v1/monitoring/ui-reload")]
		public async ValueTask Reload()
		{
			_codeService.ReloadFromFilesystem();

			Response.ContentType = "application/json";

			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII("{\"success\":1}");
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}

		/// <summary>
		/// Lists all available static files.
		/// </summary>
		[Route("/pack/static-assets/list.json")]
		public async ValueTask<List<StaticFileInfo>> GetStaticFileList()
		{
			var set = await _codeService.GetStaticFiles();
			return set;
		}

		/// <summary>
		/// Gets the email main.js file (site locale 1). The URL should be of the form /pack/email-static/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/email-static/main.js")]
		public async ValueTask<FileResult> GetEmailMainJs()
		{
			// Get locale ID from get arg called "lid". If it isn't specified, must use default locale 1.
			// Note that it would not be up to this code to decide a suitable locale if it is not specified.
			uint localeId = 1;

			if (Request.Query.TryGetValue("lid", out StringValues value))
			{
				if(!uint.TryParse(value.ToString(), out localeId))
				{
					localeId = 1;
				}
			}

			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetEmailMainJs(localeId);

			if (file.FileContent == null)
			{
				// 404
				Response.StatusCode = 404;
				return null;
			}

			if (file.Precompressed != null)
			{
				Response.Headers["Content-Encoding"] = "gzip";
				return File(file.Precompressed, "text/javascript; charset=UTF-8");
			}

			return File(file.FileContent, "text/javascript; charset=UTF-8");
		}

#if DEBUG
		/// <summary>
		/// Gets global scss (debug dev builds only) so it can be seen. Bundle is e.g. "ui" or "admin".
		/// </summary>
		/// <returns></returns>
		[Route("/pack/scss/{bundle}")]
		public string GetGlobalScss(string bundle)
		{
			var file = _codeService.GetGlobalScss(bundle);
			return file;
		}
#endif

		/// <summary>
		/// Gets the main.js file (site locale 1). The URL should be of the form /pack/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/main.js")]
		public async ValueTask<FileResult> GetMainJs()
		{
			// Get locale ID from get arg called "lid". If it isn't specified, must use default locale 1.
			// Note that it would not be up to this code to decide a suitable locale if it is not specified.
			uint localeId = 1;

			if (Request.Query.TryGetValue("lid", out StringValues value))
			{
				if (!uint.TryParse(value.ToString(), out localeId))
				{
					localeId = 1;
				}
			}

			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetMainJs(localeId);

			if (file.FileContent == null)
			{
				// 404
				Response.StatusCode = 404;
				return null;
			}

			if (file.Precompressed != null)
			{
				Response.Headers["Content-Encoding"] = "gzip";
				return File(file.Precompressed, "text/javascript; charset=UTF-8", file.LastModifiedUtc, file.Etag);
			}

			return File(file.FileContent, "text/javascript; charset=UTF-8", file.LastModifiedUtc, file.Etag);
		}

		/// <summary>
		/// Gets the main.js file for the admin area (site locale 1). The URL should be of the form /en-admin/pack/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/pack/main.js")]
		public async ValueTask<FileResult> GetAdminMainJs()
		{
			// Get locale ID from get arg called "lid". If it isn't specified, must use default locale 1.
			// Note that it would not be up to this code to decide a suitable locale if it is not specified.
			uint localeId = 1;

			if (Request.Query.TryGetValue("lid", out StringValues value))
			{
				if(!uint.TryParse(value.ToString(), out localeId))
				{
					localeId = 1;
				}
			}

			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetAdminMainJs(localeId);

			if (file.FileContent == null)
			{
				// 404
				Response.StatusCode = 404;
				return null;
			}

			if (file.Precompressed != null)
			{
				Response.Headers["Content-Encoding"] = "gzip";
				return File(file.Precompressed, "text/javascript; charset=UTF-8");
			}

			return File(file.FileContent, "text/javascript; charset=UTF-8");
		}

		/// <summary>
		/// Gets the main.css file for the ui (site locale 1). The URL should be of the form /pack/main.css?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID (currently unused), v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/main.css")]
		public async ValueTask<FileResult> GetMainCss()
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetMainCss(1);

			if (file.FileContent == null)
			{
				// 404
				Response.StatusCode = 404;
				return null;
			}

			if (file.Precompressed != null)
			{
				Response.Headers["Content-Encoding"] = "gzip";
				return File(file.Precompressed, "text/css; charset=UTF-8", file.LastModifiedUtc, file.Etag);
			}
			
			return File(file.FileContent, "text/css; charset=UTF-8", file.LastModifiedUtc, file.Etag);
		}
		/// <summary>
		/// Gets the main.css file for the admin area (site locale 1). The URL should be of the form /en-admin/pack/main.css?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID (currently unused), v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/pack/main.css")]
		public async ValueTask<FileResult> GetAdminMainCss()
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetAdminMainCss(1);

			if (file.FileContent == null)
			{
				// 404
				Response.StatusCode = 404;
				return null;
			}

			if (file.Precompressed != null)
			{
				Response.Headers["Content-Encoding"] = "gzip";
				return File(file.Precompressed, "text/css; charset=UTF-8");
			}

			return File(file.FileContent, "text/css; charset=UTF-8");
		}
	}

}
