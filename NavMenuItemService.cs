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
		private readonly Query<NavMenuItem> listByMenuQuery;
		private readonly NavMenuService _navMenus;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuItemService(NavMenuService navMenus) : base(Events.NavMenuItem)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listByMenuQuery = Query.List<NavMenuItem>();
			listByMenuQuery.Where().EqualsArg("NavMenuId", 0);
			_navMenus = navMenus;
			InstallAdminPages(null, null, new string[] { "id", "target" });
		}
		
		/// <summary>
		/// List nav menu items by menu.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<List<NavMenuItem>> ListByMenu(Context context, int menuId)
		{
			var menuSet = await _database.List(context, listByMenuQuery, null, menuId);
			menuSet = await Events.NavMenuItem.AfterList.Dispatch(context, menuSet);
			return menuSet;
		}
	}
    
}
