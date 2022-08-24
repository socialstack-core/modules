namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Event group for a bundle of events on AutoServices.
		/// </summary>
		public static Startup.ServiceEventGroup Service;
		
	}

}

namespace Api.Startup
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class ServiceEventGroup : Eventing.EventGroupCore<AutoService, uint>
	{

		/// <summary>
		/// Called just after all services have started for the first time, exactly once.
		/// Note that services may start at random points in the future, and this handler won't be invoked then - don't use this event to e.g. loop over available services.
		/// Instead, handle the Create and Delete events.
		/// </summary>
		public Api.Eventing.EventHandler<object> AfterStart;

	}

}
