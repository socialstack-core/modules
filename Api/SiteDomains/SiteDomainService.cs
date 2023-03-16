using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.SiteDomains
{
	/// <summary>
	/// Handles websites with more than one domain name. 
	/// Each user is associated with a domain when the user is created and a context extension informs 
	/// the API which domain is effectively in use.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SiteDomainService : AutoService<SiteDomain>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SiteDomainService() : base(Events.SiteDomain)
        {
			InstallAdminPages("Site Domains", "fa:fa-network-wired", new string[] { "id", "domain" });
			Cache();
		}
	}
    
}
