using Api.NavMenuItems;
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
		/// Set of events for a NavMenuItem.
		/// </summary>
		public static EventGroup<NavMenuItem> NavMenuItem;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new nav menu item.
		/// </summary>
		public static EndpointEventHandler<NavMenuItemAutoForm> NavMenuItemCreate;
		/// <summary>
		/// Delete an nav menu item.
		/// </summary>
		public static EndpointEventHandler<NavMenuItem> NavMenuItemDelete;
		/// <summary>
		/// Update nav menu item metadata.
		/// </summary>
		public static EndpointEventHandler<NavMenuItemAutoForm> NavMenuItemUpdate;
		/// <summary>
		/// Load nav menu item metadata.
		/// </summary>
		public static EndpointEventHandler<NavMenuItem> NavMenuItemLoad;
		/// <summary>
		/// List nav menu items.
		/// </summary>
		public static EndpointEventHandler<Filter<NavMenuItem>> NavMenuItemList;

		#endregion

	}

}
