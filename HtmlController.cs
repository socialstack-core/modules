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
		private HtmlService _htmlService;
		private ContextService _contexts;

		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		private static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
			var context = Request.GetContext();
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
			var context = Request.GetContext();

			var cookieRole = context.RoleId;

			if (context.UserId == 0)
			{
				// Anonymous - fire off the anon user event:
				context = await Events.ContextAfterAnonymous.Dispatch(context, context, Response);

				if (context == null)
				{
					Response.StatusCode = 404;
				}

				// Update cookie role:
				cookieRole = context.RoleId;
			}

			if (context.RoleId != cookieRole)
			{
				if(_contexts == null)
                {

                }


				// Force reset if role changed. Getting the public context will verify that the roles match.
				Response.Cookies.Append(
					_contexts.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Domain = _contexts.GetDomain(),
						IsEssential = true,
						Expires = ThePast
					}
				);

				Response.Cookies.Append(
					_contexts.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = ThePast
					}
				);

				Response.StatusCode = 404;
			}
			else
			{
				// Update the token:
				context.SendToken(Response);
			}




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
