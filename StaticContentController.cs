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
			
			var pathToAdminDir = AppSettings.Configuration["Admin"];

			if (string.IsNullOrEmpty(pathToAdminDir))
			{
				// The en-admin subdir is to make configuring NGINX easy:
				pathToAdminDir = "Admin/public/en-admin";
			}

			// User could edit the file themselves, 
			// but also other modules such as the UI watcher may also inject changes too, so we'll watch for any changes on it.
			WatchForIndexChanges(pathToUIDir, (byte[] file) => {

				// This runs immediately, and also whenever the file changes.
				_indexFile = file;

			});

			// Same for the admin index file:
			WatchForIndexChanges(pathToAdminDir, (byte[] file) => {

				// This runs immediately, and also whenever the file changes.
				_adminIndexFile = file;

			});
		}

		/// <summary>
		/// Watches the given file path for changes. Runs the given action whenever it changes, as well as immediately.
		/// </summary>
		/// <param name="directoryPath"></param>
		/// <param name="onFileReadyOrChanged"></param>
		private void WatchForIndexChanges(string directoryPath, Action<byte[]> onFileReadyOrChanged)
		{
			var fullFilePath = directoryPath + "/index.html";

			if (System.IO.File.Exists(fullFilePath))
			{
				// Start watching the file:
				using (FileSystemWatcher watcher = new FileSystemWatcher())
				{
					watcher.Path = directoryPath;

					// Watch for changes in LastAccess and LastWrite times, and
					// the renaming of files or directories.
					watcher.NotifyFilter = NotifyFilters.LastAccess
										 | NotifyFilters.LastWrite
										 | NotifyFilters.FileName
										 | NotifyFilters.DirectoryName;

					// Only watch the index.html file.
					watcher.Filter = "index.html";

					// Add event handlers.
					watcher.Changed += (object source, FileSystemEventArgs e) => {

						onFileReadyOrChanged(System.IO.File.ReadAllBytes(fullFilePath));

					};

					// Initial run:
					onFileReadyOrChanged(System.IO.File.ReadAllBytes(fullFilePath));

					// Begin watching.
					watcher.EnableRaisingEvents = true;
				}

			}
			else
			{
				// The file doesn't exist.
				onFileReadyOrChanged(
					System.Text.Encoding.UTF8.GetBytes("index.html file is missing. Restart the API after correcting this. Check the logs for the location it is expected to be.")
				);
				System.Console.WriteLine("[WARNING] Your index.html file is missing. Create it and then restart the API. Tried to find it here: " + fullFilePath);
			}

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
