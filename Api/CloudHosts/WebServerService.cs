using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Configuration;
using Api.Eventing;
using System;

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
		private WebServer platform;
		private DomainCertificateService _certs;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public WebServerService(DomainCertificateService certs)
        {
			_certs = certs;
			platform = new NGINX(this);
			
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
		/// Get the latest cert info. Will generate incomplete info if they haven't been obtained yet.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, DomainCertificateLocales> GetCertificateInfo()
		{
			// Get latest:
			var latestCerts = _certs.GetLatestCertificates();

			if (latestCerts != null)
			{
				return latestCerts;
			}

			// Generating a temporary set without the certificates
			// When the certs are available it is expected to cause the webserver service to update again.
			latestCerts = new Dictionary<string, DomainCertificateLocales>();

			var publicUrls = AppSettings.GetRawPublicUrls();

			if (publicUrls == null)
			{
				return latestCerts;
			}

			// For each public URL, check its cert.
			for (var i = 0; i < publicUrls.Length; i++)
			{
				var publicUrl = publicUrls[i];

				if (string.IsNullOrEmpty(publicUrl))
				{
					continue;
				}

				if (!publicUrl.StartsWith("https://"))
				{
					continue;
				}

				// Get the host:
				var parsedUrl = new Uri(publicUrl);
				var host = parsedUrl.Host;

				if (!latestCerts.TryGetValue(host, out DomainCertificateLocales locales))
				{
					locales.Add((uint)(i + 1));
				}
				else
				{
					locales = new DomainCertificateLocales() { Host = host };
					locales.Add((uint)(i + 1));
					latestCerts.Add(host, locales);
				}

			}

			return latestCerts;
		}

		/// <summary>
		/// Informs the webserver that the certificate set has updated. It might have not changed at all.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="certSet"></param>
		/// <returns></returns>
		public async ValueTask UpdateCertificates(Context context, Dictionary<string, DomainCertificateLocales> certSet)
		{
			try
			{
				await platform.UpdateCertificates(context, certSet);
			}
			catch (Exception ex)
			{
				Log.Error(LogTag, ex, "Unable to update certificates");
			}
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
