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
		private readonly Query<NavMenuItem> deleteItemsQuery;
		private readonly Query<NavMenu> selectByKeyQuery;

		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuService() : base(Events.NavMenu)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteItemsQuery = Query.Delete<NavMenuItem>();
			deleteItemsQuery.Where().EqualsArg("NavMenuId", 0);
			
			selectByKeyQuery = Query.Select<NavMenu>();
			selectByKeyQuery.Where().EqualsArg("Key", 0);
			
			InstallAdminPages("Nav Menus", "fa:fa-map-signs", new string[] { "id", "name", "key" });
			
			Cache(new CacheConfig<NavMenu>(){
				PreloadPriority = 20
			});
		}
		
        /// <summary>
        /// Deletes a nav menu by its ID.
        /// </summary>
        /// <returns></returns>
        public override async ValueTask<bool> Delete(Context context, int id)
        {
            await base.Delete(context, id);
			
			// Delete items too:
			await _database.Run(context, deleteItemsQuery, id);
			
			// Ok!
			return true;
        }
		
		/// <summary>
		/// Gets a single nav menu by its key.
		/// </summary>
		public async ValueTask<NavMenu> Get(Context context, string menuKey)
		{
			// Get the menu itself:
			var menu = await _database.Select(context, selectByKeyQuery, menuKey);

			if (menu == null)
			{
				// Doesn't exist.
				return null;
			}
			
			return menu;
		}
	}
    
}
