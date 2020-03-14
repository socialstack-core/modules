using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.NavMenus;

namespace Api.NavMenuItems
{
	/// <summary>
	/// Handles navigation menu items.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuItemService : AutoService<NavMenuItem>, INavMenuItemService
	{
		private readonly Query<NavMenuItem> listByMenuQuery;
		private readonly INavMenuService _navMenus;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuItemService(INavMenuService navMenus) : base(Events.NavMenuItem)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listByMenuQuery = Query.List<NavMenuItem>();
			listByMenuQuery.Where().EqualsArg("NavMenuId", 0);
			_navMenus = navMenus;
		}
		
		/// <summary>
		/// List nav menu items by menu.
		/// </summary>
		/// <returns></returns>
		public async Task<List<NavMenuItem>> ListByMenu(Context context, int menuId)
		{
			var menuSet = await _database.List(context, listByMenuQuery, null, menuId);
			menuSet = await Events.NavMenuItem.AfterList.Dispatch(context, menuSet);
			return menuSet;
		}

		/// <param name="targetUrl">The target page url, e.g. /en-admin/page</param>
		/// <param name="iconRef">The ref to use for the icon. Typically these are fontawesome refs, of the form fa:fa-thing</param>
		/// <param name="label">The text that appears on the menu</param>
		public async Task InstallAdminEntry(string targetUrl, string iconRef, string label)
		{
			var bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(new {
				content = label
			});

			await Install(
				new NavMenuItem
				{
					Target = targetUrl,
					MenuKey = "admin_primary",
					BodyJson = bodyJson,
					IconRef = iconRef
				}
			);
		}

		/// <summary>
		/// Installs an item (Creates it if it doesn't already exist). MenuKey is required, but MenuId is not.
		/// </summary>
		public async Task Install(NavMenuItem item)
		{
			var context = new Context();
			
			if (item.NavMenuId == 0)
			{
				var menu = await _navMenus.Get(context, item.MenuKey);

				if (menu == null)
				{
					// Navigation menu doesn't exist.
					
					if(item.MenuKey == "admin_primary"){
						// Auto create the admin primary nav now.
						menu = new NavMenu(){
							Key = "admin_primary",
							Name = "Admin Primary Navigation"
						};
						
						await _navMenus.Create(context, menu);
						
						// Create its homepage entry too:
						await Create(context, new NavMenuItem(){
							MenuKey = menu.Key,
							NavMenuId = menu.Id,
							BodyJson = "{\"content\":\"Dashboard\"}",
							Target = "/en-admin/",
							IconRef = "fa:fa-bars"
						});
						
					}else{
						System.Console.WriteLine("[WARN] Attempted to install a navigation menu into a menu that doesn't exist - the menu has this key: " + item.MenuKey);
						return;
					}
				}
				
				item.NavMenuId = menu.Id;
			}

			// Get the set of pages which we'll match by ID:
			if (item.Id != 0)
			{
				var exists = await Get(context, item.Id);

				if (exists != null)
				{
					return;
				}
						
				await Create(context, item);
				return;
			}
				
			if (item.Id == 0)
			{
				// Match by target URL of the item.
				var filter = new Filter<NavMenuItem>();
				filter.Equals("Target", item.Target).And().Equals("MenuKey", item.MenuKey);
					
				var existingEntry = (await List(context, filter));

				if (existingEntry.Count == 0)
				{
					await Create(context, item);
				}
			}
		}

	}
    
}
