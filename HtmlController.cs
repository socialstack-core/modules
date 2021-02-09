using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;

namespace Api.Pages
{
	/// <summary>
	/// This is the main frontend controller - its job is to serve html for URLs.
	/// If you're looking for the handlers for /content/ etc, you'll find that over in Api/Uploads/EventListener.cs
	/// </summary>

	public partial class HtmlController : Controller
    {
		private HtmlService _htmlService;

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
			Response.ContentType = "text/html";
			Response.Headers["Content-Encoding"] = "gzip";
			Response.Headers["Cache-Control"] = "max-age=315360000, public";
			var adminIndex = new byte[0];
			Response.ContentLength = adminIndex.Length;
            await Response.Body.WriteAsync(adminIndex, 0, adminIndex.Length);
        }
		
		/// <summary>
		/// The catch all handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("{*url}", Order = 9999)]
		public async Task CatchAll()
		{
			var context = Request.GetContext();
			var compress = true;

			Response.ContentType = "text/html";
			Response.Headers["Cache-Control"] = "no-cache";

			if (compress)
			{
				Response.Headers["Content-Encoding"] = "gzip";
			}

			await _htmlService.BuildPage(context, Request.Path, Response.Body, compress);
		}
		
	}

}
