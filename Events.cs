using Api.Huddles;
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
		/// <summary>
		/// Set of events for a huddle.
		/// </summary>
		public static EventGroup<Huddle> Huddle;
		
		/// <summary>
		/// Set of events for a huddleServer.
		/// </summary>
		public static EventGroup<HuddleServer> HuddleServer;
		
		/// <summary>
		/// Set of events for a huddleLoadMetric.
		/// </summary>
		public static EventGroup<HuddleLoadMetric> HuddleLoadMetric;
		
		/// <summary>
		/// Set of events for a huddlePermittedUser.
		/// </summary>
		public static EventGroup<HuddlePermittedUser> HuddlePermittedUser;
	}
}