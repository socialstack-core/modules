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
	/// Handles navigation menus.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuService : AutoService<NavMenu>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuService() : base(Events.NavMenu)
        {
			InstallAdminPages(
				"Nav Menus", "fa:fa-map-signs", new string[] { "id", "name", "key" },

				// Each navmenu page also has a list of navmenuitem's on it:
				new ChildAdminPageOptions(){
					ChildType = "NavMenuItem",
					Fields = new string[] { "bodyJson" }
				}
			);
		}
	}
    
}
