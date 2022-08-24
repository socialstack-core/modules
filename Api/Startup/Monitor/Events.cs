using Api.Startup;
using Newtonsoft.Json.Linq;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for the monitor service.
		/// </summary>
		public static MonitorEventGroup Monitor;
		
	}
	
	/// <summary>
	/// Event group for the monitor service.
	/// </summary>
	public class MonitorEventGroup : Eventing.EventGroupCore<HostDetails, uint>
	{
		/// <summary>
		/// Called whilst the monitor service is being setup.
		/// This is the best place to populate the host data with custom overrides, such as the hostname or group.
		/// </summary>
		public Api.Eventing.EventHandler<HostDetails> Setup;
		
		/// <summary>
		/// Called when the remote monitor service replied with something. 
		/// You can pass whatever necessary here, however, "url" and "key" are reserved to indicate a url/ key/ both change.
		/// It's recommended to put custom responses in a field named after your service to be fully future proof.
		/// </summary>
		public Api.Eventing.EventHandler<JObject> AfterReply;

	}
}
