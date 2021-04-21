using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;
using System.Text;
using Api.Eventing;
using Api.Startup;

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
		/// The catch all admin panel handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/{*url}", Order = 9998)]
		public async Task CatchAllAdmin()
		{
			var context = await Request.GetContext();
			var compress = true;

			Response.ContentType = "text/html";
			Response.Headers["Cache-Control"] = "no-store";

			if (compress)
			{
				Response.Headers["Content-Encoding"] = "gzip";
			}

			await _htmlService.BuildPage(context, Request.Path, Response.Body, compress);
		}
		
		/// <summary>
		/// The catch all handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("{*url}", Order = 9999)]
		public async Task CatchAll()
		{
			var context = await Request.GetContext();

			var cookieRole = context.RoleId;

			await context.RoleCheck(Request, Response);

			// Update the token:
			context.SendToken(Response);

			var compress = true;

			Response.ContentType = "text/html";
			Response.Headers["Cache-Control"] = "no-store";

			if (compress)
			{
				Response.Headers["Content-Encoding"] = "gzip";
			}

			await _htmlService.BuildPage(context, Request.Path, Response.Body, compress);
		}

		/// <summary>
		/// Gets or generates the robots.txt file.
		/// </summary>
		/// <returns></returns>
		[Route("robots.txt")]
		public FileResult Robots()
		{
			// Robots.txt as a byte[]:
			var robots = _htmlService.GetRobotsTxt();
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
