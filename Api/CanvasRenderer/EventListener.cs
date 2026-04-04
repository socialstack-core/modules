using System;
using System.IO;
using Api.Startup;
using Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Api.Signatures;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Api.Eventing;
using Api.Contexts;

namespace Api.CanvasRenderer
{

	/// <summary>
	/// Listens for events to setup the development pack directory.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the configure app method:
			Events.WebServerStartup.BeforeConfigureApplication.AddEventListener((Context context,IApplicationBuilder app) => {

				// Hook up the static dirs:

				var pubPath = Path.GetFullPath("UI/Source");

				if (Directory.Exists(pubPath))
				{
					app.UseStaticFiles(new StaticFileOptions()
					{
						FileProvider = new PhysicalFileProvider(pubPath),
						RequestPath = new PathString("/pack/static"),
						ServeUnknownFileTypes = true
					});
				}

				pubPath = Path.GetFullPath("Admin/Source");

				if (Directory.Exists(pubPath))
				{
					app.UseStaticFiles(new StaticFileOptions()
					{
						FileProvider = new PhysicalFileProvider(pubPath),
						RequestPath = new PathString("/en-admin/pack/static"),
						ServeUnknownFileTypes = true
					});
				}

				return new ValueTask<IApplicationBuilder>(app);
			});
		}
	}
}
