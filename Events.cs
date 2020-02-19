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

		#region Service events

		/// <summary>
		/// Set of events for a NavMenu.
		/// </summary>
		public static EventGroup<NavMenu> NavMenu;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new nav menu.
		/// </summary>
		public static EndpointEventHandler<NavMenuAutoForm> NavMenuCreate;
		/// <summary>
		/// Delete an nav menu.
		/// </summary>
		public static EndpointEventHandler<NavMenu> NavMenuDelete;
		/// <summary>
		/// Update nav menu metadata.
		/// </summary>
		public static EndpointEventHandler<NavMenuAutoForm> NavMenuUpdate;
		/// <summary>
		/// Load nav menu metadata.
		/// </summary>
		public static EndpointEventHandler<NavMenu> NavMenuLoad;
		/// <summary>
		/// List nav menus.
		/// </summary>
		public static EndpointEventHandler<Filter<NavMenu>> NavMenuList;

		#endregion

	}

}
