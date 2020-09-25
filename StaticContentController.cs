using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;

namespace Api.Uploader
{
	/// <summary>
	/// Note that this is best done with a reverse proxy webserver like NGINX.
	/// This controller catches all page requests and ensures they are served with the main index.html file.
	/// If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
	/// </summary>

	public partial class StaticContentController : Controller
    {
		private StaticContentService _staticContentService;
		
		/// <summary>
		/// Instanced automatically per request.
		/// </summary>
		/// <param name="scs"></param>
		public StaticContentController(StaticContentService scs)
		{
			_staticContentService = scs;
		}
		
		/// <summary>
		/// The catch all admin panel handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/{*url}", Order = 9998)]
		public async void CatchAllAdmin()
		{
			Response.ContentType = "text/html";
			Response.Headers["Content-Encoding"] = "gzip";
			Response.Headers["Cache-Control"] = "max-age=315360000, public";
			var adminIndex = _staticContentService.AdminIndex;
			Response.ContentLength = adminIndex.Length;
            await Response.Body.WriteAsync(adminIndex, 0, adminIndex.Length);
        }
		
		/// <summary>
		/// The catch all handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("{*url}", Order = 9999)]
		public async void CatchAll()
		{
			Response.ContentType = "text/html";
			Response.Headers["Content-Encoding"] = "gzip";
			Response.Headers["Cache-Control"] = "max-age=315360000, public";
			var index = _staticContentService.Index;
			Response.ContentLength = index.Length;
			await Response.Body.WriteAsync(index, 0, index.Length);
		}
		
	}

}
