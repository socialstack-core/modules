using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Redirects;

namespace Api.CloudHosts
{
	
	/// <summary>
	/// NGINX specific configuration.
	/// </summary>
	public partial class NGINX : WebServer
    {
		private RedirectService _redirectService;
		/// <summary>
		/// Applies config changes and then performs a reload.
		/// </summary>
		/// <returns></returns>
		public override async ValueTask Apply(Context context)
		{

			_redirectService = Services.Get<RedirectService>(); // In Startup namespace
																// Start constructing new NGINX config.
			var configFile = new Api.CloudHosts.NGINXConfigFile();

			// Apply the default config in to it.
			configFile.SetupDefaults();

			// Add the redirects - get them all from the DB:
			var redirects = await _redirectService.Where("", DataOptions.IgnorePermissions).ListAll(context);

			var httpsContext = configFile.GetHttpsContext();

			// For each redirect:
			foreach (var redirect in redirects)
			{

				var from = redirect.From;
				var to = redirect.To;

				// From & to originate from the admin panel.
				// They could be null, empty strings etc.
				if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
				{
					// Ignore empty ones.
					continue;
				}

				// Add 2 location contexts to the NGINX config:
				httpsContext.AddLocationContext($"= " + from).AddDirective($"return", "301 " + to);
				httpsContext.AddLocationContext($"= " + from + "/").AddDirective($"return", "301 " + to);

			}

			// Write it out:
			configFile.WriteToFile();

			// Tell NGINX to reload:
			await Reload();

		}

		/// <summary>
		/// Tells the webserver to reload config live. On supported servers this results in no downtime.
		/// Unsupported servers will perform a restart instead.
		/// </summary>
		public override async ValueTask Reload()
		{
			await CommandLine.Execute("nginx -s reload");
		}

		/// <summary>
		/// Stop/starts the web server service. Causes some downtime unlike Reload does.
		/// </summary>
		public override async ValueTask Restart()
		{
			await CommandLine.Execute("sudo service nginx restart");
		}

	}
    
}
