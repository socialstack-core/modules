using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.NavMenuItems;


namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menus.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuService : INavMenuService
	{
        private IDatabaseService _database;
		
		private readonly Query<NavMenu> deleteQuery;
		private readonly Query<NavMenuItem> deleteItemsQuery;
		private readonly Query<NavMenu> createQuery;
		private readonly Query<NavMenu> selectQuery;
		private readonly Query<NavMenu> selectByKeyQuery;
		private readonly Query<NavMenu> listQuery;
		private readonly Query<NavMenu> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<NavMenu>();

			deleteItemsQuery = Query.Delete<NavMenuItem>();
			deleteItemsQuery.Where().EqualsArg("NavMenuId", 0);
			
			createQuery = Query.Insert<NavMenu>();
			updateQuery = Query.Update<NavMenu>();
			selectQuery = Query.Select<NavMenu>();
			listQuery = Query.List<NavMenu>();

			selectByKeyQuery = Query.Select<NavMenu>();
			selectByKeyQuery.Where().EqualsArg("Key", 0);
		}
		
        /// <summary>
        /// Deletes a nav menu by its ID.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id)
        {
            // Delete the menu entry:
			await _database.Run(deleteQuery, id);
				
			// Delete items too:
			await _database.Run(deleteItemsQuery, id);
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List a filtered set of nav menus.
		/// </summary>
		/// <returns></returns>
		public async Task<List<NavMenu>> List(Context context, Filter<NavMenu> filter)
		{
			filter = await Events.NavMenuBeforeList.Dispatch(context, filter);
			
			var menuSet = await _database.List(listQuery, filter);
			
			menuSet = await Events.NavMenuAfterList.Dispatch(context, menuSet);
			return menuSet;
		}

		/// <summary>
		/// Gets a single nav menu by its ID.
		/// </summary>
		public async Task<NavMenu> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Gets a single nav menu by its key.
		/// </summary>
		public async Task<NavMenu> Get(Context context, string menuKey)
		{
			// Get the menu itself:
			var menu = await _database.Select(selectByKeyQuery, menuKey);

			if (menu == null)
			{
				// Doesn't exist.
				return null;
			}
			
			return menu;
		}

		/// <summary>
		/// Creates a new nav menu.
		/// </summary>
		public async Task<NavMenu> Create(Context context, NavMenu navMenu)
		{
			navMenu = await Events.NavMenuBeforeCreate.Dispatch(context, navMenu);

			// Note: The Id field is automatically updated by Run here.
			if (navMenu == null || !await _database.Run(createQuery, navMenu))
			{
				return null;
			}

			navMenu = await Events.NavMenuAfterCreate.Dispatch(context, navMenu);
			return navMenu;
		}

		/// <summary>
		/// Updates the given nav menu.
		/// </summary>
		public async Task<NavMenu> Update(Context context, NavMenu navMenu)
		{
			navMenu = await Events.NavMenuBeforeUpdate.Dispatch(context, navMenu);

			if (navMenu == null || !await _database.Run(updateQuery, navMenu, navMenu.Id))
			{
				return null;
			}

			navMenu = await Events.NavMenuAfterUpdate.Dispatch(context, navMenu);
			return navMenu;
		}
	}
    
}
