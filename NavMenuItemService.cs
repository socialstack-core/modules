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
	public partial class NavMenuItemService : INavMenuItemService
	{
        private IDatabaseService _database;
		
		private readonly Query<NavMenuItem> deleteQuery;
		private readonly Query<NavMenuItem> createQuery;
		private readonly Query<NavMenuItem> selectQuery;
		private readonly Query<NavMenuItem> listQuery;
		private readonly Query<NavMenuItem> listByMenuQuery;
		private readonly Query<NavMenuItem> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuItemService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<NavMenuItem>();
			
			createQuery = Query.Insert<NavMenuItem>();
			updateQuery = Query.Update<NavMenuItem>();
			selectQuery = Query.Select<NavMenuItem>();
			listQuery = Query.List<NavMenuItem>();
			listByMenuQuery = Query.List<NavMenuItem>();
			listByMenuQuery.Where().EqualsArg("NavMenuId", 0);
		}
		
        /// <summary>
        /// Deletes a nav menu by its ID.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id)
        {
            // Delete the menu entry:
			await _database.Run(deleteQuery, id);
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List nav menu items by menu.
		/// </summary>
		/// <returns></returns>
		public async Task<List<NavMenuItem>> ListByMenu(Context context, int menuId)
		{
			var menuSet = await _database.List(listByMenuQuery, null, menuId);
			menuSet = await Events.NavMenuItemAfterList.Dispatch(context, menuSet);
			return menuSet;
		}

		/// <summary>
		/// List a filtered set of nav menu items.
		/// </summary>
		/// <returns></returns>
		public async Task<List<NavMenuItem>> List(Context context, Filter<NavMenuItem> filter)
		{
			filter = await Events.NavMenuItemBeforeList.Dispatch(context, filter);
			
			var menuSet = await _database.List(listQuery, filter);
			
			menuSet = await Events.NavMenuItemAfterList.Dispatch(context, menuSet);
			return menuSet;
		}

		/// <summary>
		/// Gets a single nav menu by its ID.
		/// </summary>
		public async Task<NavMenuItem> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new nav menu item.
		/// </summary>
		public async Task<NavMenuItem> Create(Context context, NavMenuItem navMenu)
		{
			navMenu = await Events.NavMenuItemBeforeCreate.Dispatch(context, navMenu);

			// Note: The Id field is automatically updated by Run here.
			if (navMenu == null || !await _database.Run(createQuery, navMenu))
			{
				return null;
			}

			navMenu = await Events.NavMenuItemAfterCreate.Dispatch(context, navMenu);
			return navMenu;
		}

		/// <summary>
		/// Updates the given nav menu item.
		/// </summary>
		public async Task<NavMenuItem> Update(Context context, NavMenuItem navMenu)
		{
			navMenu = await Events.NavMenuItemBeforeUpdate.Dispatch(context, navMenu);

			if (navMenu == null || !await _database.Run(updateQuery, navMenu, navMenu.Id))
			{
				return null;
			}

			navMenu = await Events.NavMenuItemAfterUpdate.Dispatch(context, navMenu);
			return navMenu;
		}
	}
    
}
