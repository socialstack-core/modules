using Api.Connections;
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
		/// Just before a new connection is created. The given connection won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Connection> ConnectionBeforeCreate;

		/// <summary>
		/// Just after an connection has been created. The given connection object will now have an ID.
		/// </summary>
		public static EventHandler<Connection> ConnectionAfterCreate;

		/// <summary>
		/// Just before an connection is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Connection> ConnectionBeforeDelete;

		/// <summary>
		/// Just after an connection has been deleted.
		/// </summary>
		public static EventHandler<Connection> ConnectionAfterDelete;

		/// <summary>
		/// Just before updating an connection. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Connection> ConnectionBeforeUpdate;

		/// <summary>
		/// Just after updating an connection.
		/// </summary>
		public static EventHandler<Connection> ConnectionAfterUpdate;

		/// <summary>
		/// Just after an connection was loaded.
		/// </summary>
		public static EventHandler<Connection> ConnectionAfterLoad;

		/// <summary>
		/// Just before a service loads an connection list.
		/// </summary>
		public static EventHandler<Filter<Connection>> ConnectionBeforeList;

		/// <summary>
		/// Just after an connection list was loaded.
		/// </summary>
		public static EventHandler<List<Connection>> ConnectionAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new connection.
		/// </summary>
		public static EndpointEventHandler<ConnectionAutoForm> ConnectionCreate;
		/// <summary>
		/// Delete an connection.
		/// </summary>
		public static EndpointEventHandler<Connection> ConnectionDelete;
		/// <summary>
		/// Update connection metadata.
		/// </summary>
		public static EndpointEventHandler<ConnectionAutoForm> ConnectionUpdate;
		/// <summary>
		/// Load connection metadata.
		/// </summary>
		public static EndpointEventHandler<Connection> ConnectionLoad;
		/// <summary>
		/// List connections.
		/// </summary>
		public static EndpointEventHandler<Filter<Connection>> ConnectionList;

		#endregion

	}

}
