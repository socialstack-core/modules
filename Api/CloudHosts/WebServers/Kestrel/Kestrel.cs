using Api.Contexts;
using Api.Startup;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.CloudHosts
{

	/// <summary>
	/// Kestrel specific configuration.
	/// </summary>
	public partial class Kestrel : WebServer
    {
		/// <summary>
		/// The webserver service this belongs to.
		/// </summary>
		public WebServerService Service;

		/// <summary>
		/// Creates a new Kestrel config manager as part of the given webserver service.
		/// </summary>
		/// <param name="service"></param>
		public Kestrel(WebServerService service)
		{
			Service = service;
		}

		/// <summary>
		/// Applies config changes and then performs a reload.
		/// </summary>
		/// <returns></returns>
		public override async ValueTask Apply(Context context)
		{
			// Get the cert info:
			var certInfo = Service.GetCertificateInfo();

			// Ensure this dir exists as it's referenced by the config.
			var kestrelConfigPath = Path.GetFullPath("kestrel");
			Directory.CreateDirectory(kestrelConfigPath);

			foreach (var kvp in certInfo)
			{
				var serviceCert = kvp.Value;

				if (serviceCert.Certificate != null)
				{
					// Ensure this certs pk and chain are written out.
					// The default engine assumes they are at ./kestrel/{host}-privkey.pem and ./kestrel/{host}-fullchain.pem
					File.WriteAllText($"{kestrelConfigPath}/" + kvp.Key + "-privkey.pem", serviceCert.Certificate.PrivateKeyPem);
					File.WriteAllText($"{kestrelConfigPath}/" + kvp.Key + "-fullchain.pem", serviceCert.Certificate.FullchainPem);
				}
			}

			await Reload();
		}

		/// <summary>
		/// Tells the webserver to reload config live. On supported servers this results in no downtime.
		/// Unsupported servers will perform a restart instead.
		/// </summary>
		public override async ValueTask Reload()
		{
			var kestrelConfigPath = Path.GetFullPath("kestrel");
			CertificateHolder.LoadFromFiles(kestrelConfigPath);
		}

		/// <summary>
		/// Stop/starts the web server service. Causes some downtime unlike Reload does.
		/// </summary>
		public override async ValueTask Restart()
		{
		}

	}

}
