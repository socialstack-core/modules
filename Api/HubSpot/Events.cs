using Api.Eventing;
using Api.HubSpot;
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
		/// Event group for the hubspot integration.
		/// </summary>
		public static HubSpotEventGroup HubSpot;
	}

	public partial class EventGroupCore<T, ID>
	{
		/// <summary>
		/// Called before data is passed to hubspot to allow for dynamic data to be added based in the primary object
		/// </summary>
		public EventHandler<Dictionary<string, object>, T> HubSpotProperties;
	}
}

namespace Api.HubSpot
	{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class HubSpotEventGroup : Eventing.EventGroupCore<object, uint>
	{
	}
}
