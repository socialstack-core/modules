using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Startup;
using Api.Translate;
using System.Linq;
using System.IO;
using System;

namespace Api.CloudHosts
{
	
	/// <summary>
	/// NGINX specific configuration.
	/// </summary>
	public partial class NGINX : WebServer
    {
		private LocaleService _localeService;
		private List<Locale> _allLocales;
		/// <summary>
		/// The webserver service this belongs to.
		/// </summary>
		public WebServerService Service;

		/// <summary>
		/// Creates a new NGINX config manager as part of the given webserver service.
		/// </summary>
		/// <param name="service"></param>
		public NGINX(WebServerService service)
		{
			Service = service;
		}

		/// <summary>
		/// Applies config changes and then performs a reload.
		/// </summary>
		/// <returns></returns>
		public override async ValueTask Apply(Context context)
		{
			_localeService ??= Services.Get<LocaleService>();
			
			// Start constructing new NGINX config.
			var configFile = new NGINXConfigFile();

			// Get the cert info:
			var certInfo = Service.GetCertificateInfo();

            // Ensure this dir exists as it's referenced by the config.
            var nginxConfigPath = Path.GetFullPath("nginx");
            Directory.CreateDirectory(nginxConfigPath);

			foreach (var kvp in certInfo)
			{
				var serviceCert = kvp.Value;

				if (serviceCert.Certificate != null)
				{
					// Ensure this certs pk and chain are written out.
					// SetupDefaults assumes they are at ./nginx/{host}-privkey.pem and ./nginx/{host}-fullchain.pem
					File.WriteAllText($"{nginxConfigPath}/" + kvp.Key + "-privkey.pem", serviceCert.Certificate.PrivateKeyPem);
					File.WriteAllText($"{nginxConfigPath}/" + kvp.Key + "-fullchain.pem", serviceCert.Certificate.FullchainPem);
				}
			}

			// Apply the default config in to it for the given set of hostnames.
			configFile.SetupDefaults(nginxConfigPath, certInfo);

			// Get the configurable context, the one into which we can put our custom redirect rules:
			var cfgContext = configFile.GetConfigurableContext();
			
			// case insensitive list to stop duplicate redirects
			var redirectsList = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			
			/*
			 * These redirects are obsoleted: use permalinks instead. They are much more powerful.
			 * 
			// For each redirect:
			foreach (var redirect in redirects)
			{
				var from = redirect.From.Trim();
                var to = redirect.To.Trim();
                if (redirectsList.Contains(from)) {
					Log.Error("nginx", $"Found duplicate redirect - ignoring - {from} -> {to}");
					continue;
				}

				var statusCode = redirect.PermanentRedirect ? "301 " : "302 ";

				// From & to originate from the admin panel.
				// They could be null, empty strings etc.
				if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
				{
					// Ignore empty ones.
					continue;
				}

				redirectsList.Add(from);	

				// Add 2 location contexts to the NGINX config:
				cfgContext.AddLocationContext($"= " + from).AddDirective($"return", statusCode + to);
				cfgContext.AddLocationContext($"= " + from + "/").AddDirective($"return", statusCode + to);
			}
			*/

			// locales can also be optionally redirected
			var locales = GetAllLocales(context);

			if (locales != null && locales.Count > 0)
			{
				foreach (var altLocale in locales)
				{
					if (altLocale.isRedirected && !redirectsList.Contains("/" + altLocale.Code))
					{
						var statusCode = altLocale.PermanentRedirect ? "301 " : "302 ";
						redirectsList.Add("/" + altLocale.Code);
						cfgContext.AddLocationContext($"= /" + altLocale.Code.ToLower()).AddDirective($"return", statusCode + "/"); // root
						cfgContext.AddLocationContext($"~ /" + altLocale.Code.ToLower() + "/(.*)").AddDirective($"return", statusCode + "/$1"); // underlying pages
					}
				}
			}

			// Write it out:
			configFile.WriteToFile(nginxConfigPath);

			// Tell NGINX to reload:
			await Reload();
		}

		/// <summary>
		/// Get all the active locales 
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		private List<Locale> GetAllLocales(Context ctx)
		{
			if (_allLocales != null && _allLocales.Any())
			{
				return _allLocales;
			}

			// Get all the current locales:
			var locales = _localeService.Where("").ListAll(ctx).Result;

			if (locales != null && locales.Any())
			{
				_allLocales = locales;
			}
			else
			{
				_allLocales = new List<Locale>();
			}

			return _allLocales;
		}

		/// <summary>
		/// Tells the webserver to reload config live. On supported servers this results in no downtime.
		/// Unsupported servers will perform a restart instead.
		/// </summary>
		public override async ValueTask Reload()
		{
            Log.Info("nginx", $"Reloading nginx config");
			await CommandLine.Execute("sudo nginx -s reload");
		}

		/// <summary>
		/// Stop/starts the web server service. Causes some downtime unlike Reload does.
		/// </summary>
		public override async ValueTask Restart()
		{
            Log.Info("nginx", $"Restarting nginx");
			await CommandLine.Execute("sudo service nginx restart");
		}

	}

}
