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
using Api.Startup;
using Api.Startup.Routing;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// Handles requests to /pack/* for frontend code files.
    /// </summary>
    [InternalApi]
    public partial class FrontendCodeController : AutoController
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
		public UIReloadResult Reload()
		{
			var version = _codeService.ReloadFromFilesystem();

			return new UIReloadResult() {
				Version = version
			};
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
		/// The type metadata.
		/// </summary>
		[Route("/pack/type-meta.json")]
		public async ValueTask<FileContent?> GetTypeMeta()
		{
			var file = await _codeService.GetTypeMeta();
			return ServeFile(file, "text/json; charset=UTF-8");
		}
		
		private FileContent? ServeFile(FrontendFile file, string mime)
		{
			if (file.FileContent == null)
			{
				// 404
				return null;
			}

			if (file.Precompressed != null)
			{
				return new FileContent(file.Precompressed, mime, null, true);
			}

			return new FileContent(file.FileContent, mime);
		}

		/// <summary>
		/// Gets the email main.js file (site locale 1). The URL should be of the form /pack/email-static/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/email-static/main.js")]
		public async ValueTask<FileContent?> GetEmailMainJs([FromQuery] uint localeId = 1)
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetEmailMainJs(localeId);
			return ServeFile(file, "text/javascript; charset=UTF-8");
		}

#if DEBUG
		/// <summary>
		/// Gets global scss (debug dev builds only) so it can be seen.
		/// </summary>
		/// <returns></returns>
		[Route("/pack/scss/global")]
		public FileContent GetGlobalScss()
		{
			var file = _codeService.GetScssGlobals();
			return new FileContent(Encoding.UTF8.GetBytes(file), "text/plain");
		}
#endif

		/// <summary>
		/// Gets the main.js file (site locale 1). The URL should be of the form /pack/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/main.js")]
		public async ValueTask<FileContent?> GetMainJs([FromQuery] uint localeId = 1)
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetMainJs(localeId);

			if (file.FileContent == null)
			{
				// 404
				return null;
			}

			if (file.Precompressed != null)
			{
				return new FileContent(file.Precompressed, "text/javascript; charset=UTF-8", null, true, file.LastModifiedUtcString, file.Etag);
			}

			return new FileContent(file.FileContent, "text/javascript; charset=UTF-8", null, false, file.LastModifiedUtcString, file.Etag);
		}

		/// <summary>
		/// Gets the main.js file for the admin area (site locale 1). The URL should be of the form /en-admin/pack/main.js?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID, v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/pack/main.js")]
		public async ValueTask<FileContent?> GetAdminMainJs([FromQuery] uint localeId = 1)
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetAdminMainJs(localeId);
			return ServeFile(file, "text/javascript; charset=UTF-8");
		}

		/// <summary>
		/// Gets the main.css file for the ui (site locale 1). The URL should be of the form /pack/main.css?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID (currently unused), v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/pack/main.css")]
		public async ValueTask<FileContent?> GetMainCss()
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetMainCss(1);

			if (file.FileContent == null)
			{
				// 404
				return null;
			}

			if (file.Precompressed != null)
			{
				return new FileContent(file.Precompressed, "text/css; charset=UTF-8", null, true, file.LastModifiedUtcString, file.Etag);
			}
			
			return new FileContent(file.FileContent, "text/css; charset=UTF-8", null, false, file.LastModifiedUtcString, file.Etag);
		}
		/// <summary>
		/// Gets the main.css file for the admin area (site locale 1). The URL should be of the form /en-admin/pack/main.css?loc=1&amp;v=123123123123&amp;h=ma83md83jd7hdur8
		/// Where loc is the locale ID (currently unused), v is the original code build timestamp in ms, and h is the hash of the file.
		/// For convenience, ask FrontendCodeService for the url via GetMainJsUrl(Context context).
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/pack/main.css")]
		public async ValueTask<FileContent?> GetAdminMainCss()
		{
			// Ask the service as it's almost always cached in there.
			var file = await _codeService.GetAdminMainCss(1);
			return ServeFile(file, "text/css; charset=UTF-8");
		}
	}

	/// <summary>
	/// Output from a UI reload.
	/// </summary>
	public struct UIReloadResult
	{
		/// <summary>
		/// UI version.
		/// </summary>
		public long Version;
	}

}
