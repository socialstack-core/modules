using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;
using System.Text;
using Microsoft.Extensions.Primitives;

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
			int localeId = 1;

			if (Request.Query.TryGetValue("lid", out StringValues value))
			{
				int.TryParse(value.ToString(), out localeId);
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
				return File(file.Precompressed, "text/javascript; charset=UTF-8");
			}

			return File(file.FileContent, "text/javascript; charset=UTF-8");
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
			int localeId = 1;

			if (Request.Query.TryGetValue("lid", out StringValues value))
			{
				int.TryParse(value.ToString(), out localeId);
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
		public async ValueTask<FileResult> GetAdminMainCss()
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
				return File(file.Precompressed, "text/css; charset=UTF-8");
			}
			
			return File(file.FileContent, "text/css; charset=UTF-8");
		}
		/// <summary>
		/// Gets the main.css file for the admin area (site locale 1). The URL should be of the form /en-admin/pack/main.css?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID (currently unused), v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/pack/main.css")]
		public async ValueTask<FileResult> GetMainCss()
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
