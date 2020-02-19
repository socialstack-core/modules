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
		/// All page entity events.
		/// </summary>
		public static EventGroup<Page> Page;
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
