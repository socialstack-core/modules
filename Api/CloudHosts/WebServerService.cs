﻿using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.CloudHosts
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// Handles configuring the webserver which is currently always NGINX.
	/// </summary>
	public partial class WebServerService : AutoService
    {
		/// <summary>
		/// Underlying platform. Currently it's always NGINX.
		/// </summary>
		private WebServer platform = new NGINX();
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public WebServerService(WebSecurityService secService)
        {
			// WebSecurityService has the job of ensuring certs are good.
			// It provides us with the details of the certs (their file location for example) to be then used in webserver config.

			Events.Configuration.AfterUpdate.AddEventListener(async (Context ctx, Configuration.Configuration configuration) =>
			{

				if (configuration != null && configuration.Key == "HtmlService")
				{
					await Regenerate(ctx);
				}

				return configuration;
			});


			Events.Configuration.AfterCreate.AddEventListener(async (Context ctx, Configuration.Configuration configuration) =>
			{

				if (configuration != null && configuration.Key == "HtmlService")
				{
					await Regenerate(ctx);
				}

				return configuration;
			});

			// Ensure webserver is initted.

		}

		/// <summary>
		/// Regenerates website config
		/// </summary>
		/// <returns></returns>
		public async ValueTask Regenerate(Context context)
		{
			await platform.Apply(context);
		}

	}
    
}
