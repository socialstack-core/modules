using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;

namespace Api.CloudHosts;

/// <summary>
/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
/// Used to generate TLS certificates either via a remote source or a http challenge on Let's Encrypt.
/// </summary>
public partial class WebSecurityService : AutoService
    {
	private DomainCertificateService _certs;
	private WebServerService _webServer;
	private WebSecurityConfig _config;

 	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public WebSecurityService(DomainCertificateService certs, WebServerService webServer)
        {
		_certs = certs;
		_webServer = webServer;
		_config = GetConfig<WebSecurityConfig>();

		// Daily automation to check the cert.
		Events.Automation("tls-certificate", "0 0 0 ? * * *", true)
			.AddEventListener(async (Context context, Api.Automations.AutomationRunInfo runInfo) => {

				await CheckCertificate(context);
				return runInfo;

			});

		Events.Service.AfterStart.AddEventListener(async (Context context, object obj) => {

			await CheckCertificate(context);
			return obj;

		});

	}

	/// <summary>
	/// Checks the date on installed certs and renews them if necessary.
	/// Considers that other servers in the cluster are doing the same thing at the same time.
	/// </summary>
	/// <returns></returns>
	public async ValueTask CheckCertificate(Context context)
	{
		if (Api.Configuration.Environment.IsDevelopment())
		{
			// Avoid doing cert checks on http localhost instances,
			// including ones with https:// configs present.
			return;
		}

		if (!_config.AutoCertificate)
		{
			return;
		}

		// Get the valid set of certs. This renews or obtains new ones depending on what's needed.
		var certs = await _certs.UpdateValidSet(context);

		// Inform the web server service that the certificate set has been updated.
		await _webServer.UpdateCertificates(context, certs);
	}
}

/// <summary>
/// A set of locales used a particular hostname.
/// </summary>
public class DomainCertificateLocales
{
	/// <summary>
	/// The hostname (domain).
	/// </summary>
	public string Host;

	/// <summary>
	/// The locale IDs.
	/// </summary>
	public List<uint> Locales = new List<uint>();

	/// <summary>
	/// The obtained certificate.
	/// </summary>
	public ServiceCertificate Certificate;

	/// <summary>
	/// Adds a new locale to this set.
	/// </summary>
	/// <param name="id"></param>
	public void Add(uint id)
	{
		Locales.Add(id);
	}
}