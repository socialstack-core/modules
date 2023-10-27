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
		public NavMenuItemService(NavMenuService navMenus) : base(Events.NavMenuItem)
        {
			//InstallAdminPages(null, null, new string[] { "id", "target" });

			Events.NavMenuItem.BeforeCreate.AddEventListener(async (Context context, NavMenuItem item) => {

				if (item.NavMenuId != 0)
				{
					// Get the menu:
					var parentMenu = await navMenus.Get(context, item.NavMenuId, DataOptions.IgnorePermissions);

					if (parentMenu != null)
					{
						item.MenuKey = parentMenu.Key;
					}
				}

				return item;
			});

		}
	}
    
}
