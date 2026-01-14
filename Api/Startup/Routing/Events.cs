using Api.Startup.Routing;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Event group for a bundle of events on the router.
		/// </summary>
		public static Startup.RouterEventGroup Router;
		
	}

}

namespace Api.Startup
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class RouterEventGroup : Eventing.EventGroup
	{
		/// <summary>
		/// Called to collect any custom routes on the router.
		/// </summary>
		public Api.Eventing.EventHandler<RouterBuilder> CollectRoutes;
	}

}
