using Api.Huddles;
using Api.Permissions;
using System.Collections.Generic;
using Api.Users;

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
		/// Set of events for a huddlePresence.
		/// </summary>
		public static EventGroup<HuddlePresence> HuddlePresence;
		
		/// <summary>
		/// Set of events for a huddleLoadMetric.
		/// </summary>
		public static EventGroup<HuddleLoadMetric> HuddleLoadMetric;
		
		/// <summary>
		/// Called when a huddle is being joined.
		/// </summary>
		public static EventHandler<HuddleJoinInfo, Huddle, User> HuddleGetJoinInfo;
	}
}