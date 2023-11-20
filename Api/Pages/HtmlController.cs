using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;
using System.Text;
using Api.Eventing;
using Api.Startup;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Api.Pages
{
	/// <summary>
	/// This is the main frontend controller - its job is to serve html for URLs.
	/// If you're looking for the handlers for /content/ etc, you'll find that over in Api/Uploads/EventListener.cs
	/// </summary>

	public partial class HtmlController : Controller
    {
		private static HtmlService _htmlService;

		/// <summary>
		/// Instanced automatically per request.
		/// </summary>
		/// <param name="htmlService"></param>
		public HtmlController(HtmlService htmlService)
		{
			_htmlService = htmlService;
		}

		/// <summary>
		/// Lists all available static files.
		/// </summary>
		[HttpPost("/pack/static-assets/mobile-html")]
		public async ValueTask GetMobileHtml([FromBody] MobilePageMeta mobileMeta)
		{
			var context = await Request.GetContext();

			Response.ContentType = "text/html";
			Response.Headers["Cache-Control"] = "no-store";

			await _htmlService.BuildMobileHomePage(context, Response.Body, mobileMeta);
		}

		/// <summary>
		/// RTE config popup base HTML.
		/// </summary>
		[HttpGet("/pack/rte.html")]
		public async ValueTask GetRteConfigPage()
		{
			var context = await Request.GetContext();

			Response.ContentType = "text/html";
			Response.Headers["Cache-Control"] = "no-store";

			// header only. The body is empty.
			await _htmlService.BuildHeaderOnly(context, Response.Body);
		}

		/// <summary>
		/// The catch all admin panel handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/{*url}", Order = 9998)]
		public async ValueTask CatchAllAdmin()
		{
			var context = await Request.GetContext();
			await _htmlService.BuildPage(context, Request, Response, false, true);
		}
		
		/// <summary>
		/// The catch all handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("{*url}", Order = 9999)]
		public async ValueTask CatchAll()
		{
			var context = await Request.GetContext();
			await _htmlService.BuildPage(context, Request, Response, true);
		}

		/// <summary>
		/// Gets or generates the robots.txt file.
		/// </summary>
		/// <returns></returns>
		[Route("robots.txt")]
		public async Task<FileResult> Robots()
		{
			var context = await Request.GetContext();

			// Robots.txt as a byte[]:
			var robots = _htmlService.GetRobotsTxt(context);
			return File(robots, "text/plain;charset=UTF-8");
		}

		/// <summary>
		/// Sitemap.xml
		/// </summary>
		/// <returns></returns>
		[Route("sitemap.xml")]
		public void Sitemap()
		{
			Response.StatusCode = 404;
		}

	}

}
