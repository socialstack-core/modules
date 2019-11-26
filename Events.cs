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
		/// Just before a new nav menu is created. The given nav menu won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuBeforeCreate;

		/// <summary>
		/// Just after an nav menu has been created. The given nav menu object will now have an ID.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuAfterCreate;

		/// <summary>
		/// Just before an nav menu is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuBeforeDelete;

		/// <summary>
		/// Just after an nav menu has been deleted.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuAfterDelete;

		/// <summary>
		/// Just before updating an nav menu. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuBeforeUpdate;

		/// <summary>
		/// Just after updating an nav menu.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuAfterUpdate;

		/// <summary>
		/// Just after an nav menu was loaded.
		/// </summary>
		public static EventHandler<NavMenu> NavMenuAfterLoad;

		/// <summary>
		/// Just before a service loads an navMenu list.
		/// </summary>
		public static EventHandler<Filter<NavMenu>> NavMenuBeforeList;

		/// <summary>
		/// Just after an navMenu list was loaded.
		/// </summary>
		public static EventHandler<List<NavMenu>> NavMenuAfterList;

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
