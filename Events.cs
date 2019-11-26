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
		/// Just before a new nav menu item is created. The given nav menu item won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemBeforeCreate;

		/// <summary>
		/// Just after an nav menu item has been created. The given nav menu item object will now have an ID.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemAfterCreate;

		/// <summary>
		/// Just before an nav menu item is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemBeforeDelete;

		/// <summary>
		/// Just after an nav menu item has been deleted.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemAfterDelete;

		/// <summary>
		/// Just before updating an nav menu item. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemBeforeUpdate;

		/// <summary>
		/// Just after updating an nav menu item.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemAfterUpdate;

		/// <summary>
		/// Just after an nav menu item was loaded.
		/// </summary>
		public static EventHandler<NavMenuItem> NavMenuItemAfterLoad;

		/// <summary>
		/// Just before a service loads an navMenu list.
		/// </summary>
		public static EventHandler<Filter<NavMenuItem>> NavMenuItemBeforeList;

		/// <summary>
		/// Just after an navMenu list was loaded.
		/// </summary>
		public static EventHandler<List<NavMenuItem>> NavMenuItemAfterList;

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
