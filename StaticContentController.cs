using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;


namespace Api.Uploader
{
	/// <summary>
	/// Note that this is best done with a reverse proxy webserver like NGINX.
	/// This controller catches all page requests and ensures they are served with the main index.html file.
	/// If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
	/// </summary>

	public partial class StaticContentController : Controller
    {
		/// <summary>
		/// The index file.
		/// </summary>
		private byte[] _indexFile;

		/// <summary>
		/// The index file.
		/// </summary>
		private byte[] _adminIndexFile;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public StaticContentController()
		{
			var pathToUIDir = AppSettings.Configuration["UI"];

			if (string.IsNullOrEmpty(pathToUIDir))
			{
				pathToUIDir = "UI/public";
			}

			var pathToIndexFile = pathToUIDir + "/index.html";

			var pathToAdminDir = AppSettings.Configuration["Admin"];

			if (string.IsNullOrEmpty(pathToAdminDir))
			{
				// The en-admin subdir is to make configuring NGINX easy:
				pathToAdminDir = "Admin/public/en-admin";
			}
			
			var pathToAdminIndexFile = pathToAdminDir + "/index.html";

			#warning Watch for changes here
			// User could edit the file themselves, 
			// but also other modules such as the UI watcher may also inject changes too.
			_indexFile = System.IO.File.ReadAllBytes(pathToIndexFile);
			
			// Same for the admin index file:
			_adminIndexFile = System.IO.File.ReadAllBytes(pathToAdminIndexFile);
		}

		/// <summary>
		/// The catch all admin panel handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("/en-admin/{*url}", Order = 9998)]
		public async void CatchAllAdmin()
		{
            Response.ContentType = "text/html";
            await Response.Body.WriteAsync(_adminIndexFile, 0, _adminIndexFile.Length);
        }
		
		/// <summary>
		/// The catch all handler. If you're looking for /content/ etc, you'll find that over in Uploads/EventListener.cs
		/// </summary>
		/// <returns></returns>
		[Route("{*url}", Order = 9999)]
		public async void CatchAll()
        {
            Response.ContentType = "text/html";
            await Response.Body.WriteAsync(_indexFile, 0, _indexFile.Length);
		}
		
	}

}
