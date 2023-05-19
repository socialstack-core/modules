using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Redirects
{
	/// <summary>
	/// Handles redirects.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RedirectService : AutoService<Redirect>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RedirectService() : base(Events.Redirect)
        {
			InstallAdminPages("Redirects", "fa:fa-reply", new string[] { "id", "from", "to" });
		}
	}
    
}
