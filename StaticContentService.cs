using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Configuration;
using System;
using System.IO;
using System.IO.Compression;

namespace Api.Uploader
{
	/// <summary>
	/// Handles static content.
	/// </summary>

	public partial class StaticContentService
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
		public StaticContentService()
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
			WatchForIndexChanges(pathToUIDir, (byte[] file) =>
			{

				// This runs immediately, and also whenever the file changes.
				_indexFile = file;

			});

			// Same for the admin index file:
			WatchForIndexChanges(pathToAdminDir, (byte[] file) =>
			{

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
					watcher.Changed += async (object source, FileSystemEventArgs e) => {
						await LoadFile(fullFilePath, onFileReadyOrChanged);
					};

					// Initial run:
					var fileBytes = System.IO.File.ReadAllBytes(fullFilePath);
					using (var ms = new MemoryStream())
					{
						using (var gs = new GZipStream(ms, CompressionMode.Compress))
						{
							gs.Write(fileBytes);
						}
						fileBytes = ms.ToArray();
					}

					onFileReadyOrChanged(fileBytes);

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
	
		private async Task LoadFile(string fullFilePath, Action<byte[]> onFileReadyOrChanged)
		{
			byte[] fileBytes = null;

			for (var i = 0; i < 10; i++)
			{
				try
				{
					fileBytes = await System.IO.File.ReadAllBytesAsync(fullFilePath);
					using (var ms = new MemoryStream()) {
						using (var gs = new GZipStream(ms, CompressionMode.Compress))
						{
							gs.Write(fileBytes);
						}
						fileBytes = ms.ToArray();
					}

					break;
				}
				catch (IOException)
				{
					// "File is in use". Wait and try again:
					await Task.Delay(100);
				}
			}

			if (fileBytes != null)
			{
				onFileReadyOrChanged(fileBytes);
			}
		}
		
		/// <summary>The admin index.html</summary>
		public byte[] AdminIndex => _adminIndexFile;
		
		/// <summary>The frontend index.html</summary>
		public byte[] Index => _indexFile;
		
		
	}

}
