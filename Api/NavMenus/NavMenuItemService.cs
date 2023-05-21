using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menu items.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuItemService : AutoService<NavMenuItem>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuItemService() : base(Events.NavMenuItem)
        {
			//InstallAdminPages(null, null, new string[] { "id", "target" });
		}
	}
    
}
