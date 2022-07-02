using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.NavMenuItems
{
	/// <summary>
	/// Handles navigation menus.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface INavMenuItemService
    {
		/// <summary>
		/// Deletes a nav menu item by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int navMenuId);

		/// <summary>
		/// Gets a single nav menu item by its ID.
		/// </summary>
		Task<NavMenuItem> Get(Context context, int navMenuId);
		
		/// <summary>
		/// Creates a new nav menu item.
		/// </summary>
		Task<NavMenuItem> Create(Context context, NavMenuItem navMenu);

		/// <summary>
		/// Updates the given nav menu item.
		/// </summary>
		Task<NavMenuItem> Update(Context context, NavMenuItem navMenu);

		/// <summary>
		/// List a filtered set of nav menu items.
		/// </summary>
		/// <returns></returns>
		Task<List<NavMenuItem>> List(Context context, Filter<NavMenuItem> filter);

		/// <summary>
		/// An optimised list method which gets nav menu items by menu ID.
		/// </summary>
		/// <returns></returns>
		Task<List<NavMenuItem>> ListByMenu(Context context, int menuId);

		/// <summary>
		/// Adds an admin nav menu entry.
		/// </summary>
		/// <param name="targetUrl">The target page url, e.g. /en-admin/page</param>
		/// <param name="iconRef">The ref to use for the icon. Typically these are fontawesome refs, of the form fa:fa-thing</param>
		/// <param name="label">The text that appears on the menu</param>
		Task InstallAdminEntry(string targetUrl, string iconRef, string label);

		/// <summary>
		/// Installs an item (Creates it if it doesn't already exist). MenuKey is required, but MenuId is not.
		/// </summary>
		Task Install(NavMenuItem item);

	}
}
