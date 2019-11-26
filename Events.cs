using Api.Pages;
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
		/// Just before a new page is created. The given page won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Page> PageBeforeCreate;

		/// <summary>
		/// Just after an page has been created. The given page object will now have an ID.
		/// </summary>
		public static EventHandler<Page> PageAfterCreate;

		/// <summary>
		/// Just before an page is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Page> PageBeforeDelete;

		/// <summary>
		/// Just after an page has been deleted.
		/// </summary>
		public static EventHandler<Page> PageAfterDelete;

		/// <summary>
		/// Just before updating an page. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Page> PageBeforeUpdate;

		/// <summary>
		/// Just after updating an page.
		/// </summary>
		public static EventHandler<Page> PageAfterUpdate;

		/// <summary>
		/// Just after an page was loaded.
		/// </summary>
		public static EventHandler<Page> PageAfterLoad;

		/// <summary>
		/// Just before a service loads an page list.
		/// </summary>
		public static EventHandler<Filter<Page>> PageBeforeList;

		/// <summary>
		/// Just after an page list was loaded.
		/// </summary>
		public static EventHandler<List<Page>> PageAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new page.
		/// </summary>
		public static EndpointEventHandler<PageAutoForm> PageCreate;
		/// <summary>
		/// Delete an page.
		/// </summary>
		public static EndpointEventHandler<Page> PageDelete;
		/// <summary>
		/// Update page metadata.
		/// </summary>
		public static EndpointEventHandler<PageAutoForm> PageUpdate;
		/// <summary>
		/// Load page metadata.
		/// </summary>
		public static EndpointEventHandler<Page> PageLoad;
		/// <summary>
		/// List pages.
		/// </summary>
		public static EndpointEventHandler<Filter<Page>> PageList;

		#endregion

	}

}
