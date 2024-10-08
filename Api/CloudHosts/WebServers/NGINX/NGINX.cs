using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Redirects;
using Api.Pages;
using Api.Translate;
using Api.Configuration;

namespace Api.CloudHosts
{
	
	/// <summary>
	/// NGINX specific configuration.
	/// </summary>
	public partial class NGINX : WebServer
    {
		private RedirectService _redirectService;
		private ConfigSet<HtmlServiceConfig> _configSet;
		private HtmlServiceConfig[] _configurationTable = new HtmlServiceConfig[0];
		private HtmlServiceConfig _defaultConfig = new HtmlServiceConfig();
		private LocaleService _localeService;

		/// <summary>
		/// Applies config changes and then performs a reload.
		/// </summary>
		/// <returns></returns>
		public override async ValueTask Apply(Context context)
		{
			_localeService = Services.Get<LocaleService>();
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
				var from = redirect.From.Trim();
				var to = redirect.To.Trim();

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

			// check - redirect primary locale (e.g. /en-gb -> /)?
			_configSet = Services.Get<HtmlService>().GetAllConfig<HtmlServiceConfig>();

			_configSet.OnChange += () =>
			{
				BuildConfigLocaleTable();
				return new ValueTask();
			};

			BuildConfigLocaleTable();
			var htmlConfig = (context.LocaleId < _configurationTable.Length) ? _configurationTable[context.LocaleId] : _defaultConfig;

			// redirect /[primary-locale-code/* to root (e.g. /en-gb/abc -> /abc)
			if (htmlConfig.RedirectPrimaryLocale)
			{
				var primaryLocale = await _localeService.Get(context, 1);
				httpsContext.AddLocationContext($"= /" + primaryLocale.Code.ToLower()).AddDirective($"return", "301 /"); // root
				httpsContext.AddLocationContext($"~ /" + primaryLocale.Code.ToLower() + "/(.*)").AddDirective($"return", "301 /$1"); // underlying pages
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

		private void BuildConfigLocaleTable()
		{
			if (_configSet == null || _configSet.Configurations == null || _configSet.Configurations.Count == 0)
			{
				// Not configured at all.
				_configurationTable = new HtmlServiceConfig[0];
				_defaultConfig = new HtmlServiceConfig();
				return;
			}

			// First collect highest locale ID.
			uint highest = 0;
			uint lowest = uint.MaxValue;

			foreach (var config in _configSet.Configurations)
			{
				if (config == null)
				{
					continue;
				}

				if (config.LocaleId > highest)
				{
					highest = config.LocaleId;
				}
				else if (config.LocaleId < lowest)
				{
					lowest = config.LocaleId;
				}
			}

			if (lowest == uint.MaxValue)
			{
				// Not configured at all.
				_configurationTable = new HtmlServiceConfig[0];
				_defaultConfig = new HtmlServiceConfig();
				return;
			}

			var ct = new HtmlServiceConfig[highest + 1];

			// Slot them:
			foreach (var config in _configSet.Configurations)
			{
				if (config == null)
				{
					continue;
				}

				ct[config.LocaleId] = config;
			}

			// Fill any gaps with the default entry. The default simply has the lowest ID (ideally 0 or 1).
			var defaultEntry = ct[lowest];

			for (var i = 0; i < ct.Length; i++)
			{
				if (ct[i] == null)
				{
					ct[i] = defaultEntry;
				}
			}

			_defaultConfig = defaultEntry;
			_configurationTable = ct;
		}

	}
    
}
