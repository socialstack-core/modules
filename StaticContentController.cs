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
		private IStaticContentService _staticContentService;
		
		/// <summary>
		/// Instanced automatically per request.
		/// </summary>
		/// <param name="scs"></param>
		public StaticContentController(IStaticContentService scs)
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
			var adminIndex = _staticContentService.AdminIndex;
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
			var index = _staticContentService.Index;
            await Response.Body.WriteAsync(index, 0, index.Length);
		}
		
	}

}
