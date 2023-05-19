using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.CloudHosts
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// Used to generate TLS certificates either via a remote source or a http challenge on Let's Encrypt.
	/// </summary>
	public partial class WebSecurityService : AutoService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public WebSecurityService()
        {

			// Daily automation to check the cert.
			Events.Automation("tls-certificate", "0 0 0 ? * * *", true)
				.AddEventListener(async (Context context, Api.Automations.AutomationRunInfo runInfo) => {

					await CheckCertificate();
					return runInfo;

				});

			Events.Service.AfterStart.AddEventListener(async (Context context, object obj) => {
				
				await CheckCertificate();
				return obj;

			});

		}

		/// <summary>
		/// Checks the date on installed certs and renews them if necessary.
		/// Considers that other servers in the cluster are doing the same thing at the same time.
		/// </summary>
		/// <returns></returns>
		private async ValueTask CheckCertificate()
		{
			
		}
	}
    
}
