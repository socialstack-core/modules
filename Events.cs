using Api.NavMenus;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a NavMenu.
		/// </summary>
		public static EventGroup<NavMenu> NavMenu;
		/// <summary>
		/// Set of events for a NavMenuItem.
		/// </summary>
		public static EventGroup<NavMenuItem> NavMenuItem;
	}
}