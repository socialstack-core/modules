using Api.CalendarEvents;
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
		/// Just before a new event is created. The given event won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Event> EventBeforeCreate;

		/// <summary>
		/// Just after an event has been created. The given event object will now have an ID.
		/// </summary>
		public static EventHandler<Event> EventAfterCreate;

		/// <summary>
		/// Just before an event is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Event> EventBeforeDelete;

		/// <summary>
		/// Just after an event has been deleted.
		/// </summary>
		public static EventHandler<Event> EventAfterDelete;

		/// <summary>
		/// Just before updating an event. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Event> EventBeforeUpdate;

		/// <summary>
		/// Just after updating an event.
		/// </summary>
		public static EventHandler<Event> EventAfterUpdate;

		/// <summary>
		/// Just after an event was loaded.
		/// </summary>
		public static EventHandler<Event> EventAfterLoad;

		/// <summary>
		/// Just before a service loads an event list.
		/// </summary>
		public static EventHandler<Filter<Event>> EventBeforeList;

		/// <summary>
		/// Just after an event list was loaded.
		/// </summary>
		public static EventHandler<List<Event>> EventAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new event.
		/// </summary>
		public static EndpointEventHandler<EventAutoForm> EventCreate;
		/// <summary>
		/// Delete an event.
		/// </summary>
		public static EndpointEventHandler<Event> EventDelete;
		/// <summary>
		/// Update event metadata.
		/// </summary>
		public static EndpointEventHandler<EventAutoForm> EventUpdate;
		/// <summary>
		/// Load event metadata.
		/// </summary>
		public static EndpointEventHandler<Event> EventLoad;
		/// <summary>
		/// List events.
		/// </summary>
		public static EndpointEventHandler<Filter<Event>> EventList;

		#endregion

	}

}
