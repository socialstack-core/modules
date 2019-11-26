using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menus.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface INavMenuService
    {
		/// <summary>
		/// Deletes a nav menu by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int navMenuId);

		/// <summary>
		/// Gets a single nav menu by its ID.
		/// </summary>
		Task<NavMenu> Get(Context context, int navMenuId);

		/// <summary>
		/// Gets a single nav menu by its key.
		/// </summary>
		Task<NavMenu> Get(Context context, string navMenuKey);

		/// <summary>
		/// Creates a new nav menu.
		/// </summary>
		Task<NavMenu> Create(Context context, NavMenu navMenu);

		/// <summary>
		/// Updates the given nav menu.
		/// </summary>
		Task<NavMenu> Update(Context context, NavMenu navMenu);

		/// <summary>
		/// List a filtered set of nav menus.
		/// </summary>
		/// <returns></returns>
		Task<List<NavMenu>> List(Context context, Filter<NavMenu> filter);

	}
}
