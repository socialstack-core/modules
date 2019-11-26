using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.NavMenus
{
    /// <summary>
    /// Used when creating or updating a nav menu
    /// </summary>
    public partial class NavMenuAutoForm : AutoForm<NavMenu>
	{
		/// <summary>
		/// A key used to identify a menu by its purpose.
		/// E.g. "primary" or "admin_primary"
		/// </summary>
		public string Key;

		/// <summary>
		/// The name of the menu, in the site default language.
		/// </summary>
		public string Name;
    }
}
