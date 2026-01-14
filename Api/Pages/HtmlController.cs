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
using Microsoft.AspNetCore.Http;
using Api.Startup.Routing;

namespace Api.Pages
{
    /// <summary>
    /// This is the main frontend controller - its job is to serve html for URLs.
    /// If you're looking for the handlers for /content/ etc, you'll find that over in Api/Uploads/EventListener.cs
    /// </summary>
    [InternalApi]
    public partial class HtmlController : AutoController
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
		
		/*
		/// <summary>
		/// Lists all available static files.
		/// </summary>
		[HttpPost("/pack/static-assets/mobile-html")]
		public async ValueTask GetMobileHtml(HttpContext httpContext, Context context, [FromBody] MobilePageMeta mobileMeta)
		{
			var response = httpContext.Response;
			response.ContentType = "text/html";
			response.Headers["Cache-Control"] = "no-store";

			await _htmlService.BuildMobileHomePage(context, response.Body, mobileMeta);
		}
		*/

		/// <summary>
		/// RTE config popup base HTML.
		/// </summary>
		[HttpGet("/pack/rte.html")]
		public async ValueTask GetRteConfigPage(HttpContext httpContext, Context context)
		{
			var response = httpContext.Response;
			response.ContentType = "text/html";
			response.Headers["Cache-Control"] = "no-store";

			// header only. The body is empty.
			await _htmlService.BuildHeaderOnly(context, response.Body);
		}

		/// <summary>
		/// Gets or generates the robots.txt file.
		/// </summary>
		/// <returns></returns>
		[Route("robots.txt")]
		public FileContent Robots(Context context)
		{
			// Robots.txt as a byte[]:
			var robots = _htmlService.GetRobotsTxt(context);
			return new FileContent(robots, "text/plain;charset=UTF-8");
		}

		/*
		/// <summary>
		/// Sitemap.xml
		/// </summary>
		/// <returns></returns>
		[Route("sitemap.xml")]
		public void Sitemap()
		{
			Response.StatusCode = 404;
		}
		*/

	}

}
