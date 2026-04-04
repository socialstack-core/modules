using Api.Database;
using Api.Users;

namespace Api.NavMenus
{
	/// <summary>
	/// Used to auto install navmenus
	/// </summary>
	public class MenuBuilder : ContentBuilder
	{
		/// <summary>
		/// The name of the menu.
		/// </summary>
		public string Name;

		/// <summary>
		/// The menu content.
		/// </summary>
		public JsonString ContentJson;
	}
}
