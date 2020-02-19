using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;

namespace Api.NavMenuItems
{
	/// <summary>
	/// Handles navigation menu items.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuItemService : AutoService<NavMenuItem>, INavMenuItemService
	{
		private readonly Query<NavMenuItem> listByMenuQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuItemService() : base(Events.NavMenuItem)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listByMenuQuery = Query.List<NavMenuItem>();
			listByMenuQuery.Where().EqualsArg("NavMenuId", 0);
		}
		
		/// <summary>
		/// List nav menu items by menu.
		/// </summary>
		/// <returns></returns>
		public async Task<List<NavMenuItem>> ListByMenu(Context context, int menuId)
		{
			var menuSet = await _database.List(listByMenuQuery, null, menuId);
			menuSet = await Events.NavMenuItem.AfterList.Dispatch(context, menuSet);
			return menuSet;
		}

	}
    
}
