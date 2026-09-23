using Api.Forums;
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
		/// Set of events for a Forum.
		/// </summary>
		public static EventGroup<Forum> Forum;
		
		/// <summary>
		/// Set of events for a ForumReply.
		/// </summary>
		public static EventGroup<ForumReply> ForumReply;
		
		/// <summary>
		/// Set of events for a ForumThread.
		/// </summary>
		public static EventGroup<ForumThread> ForumThread;
		
		/// <summary>
		/// Set of events for a ForumThreadStat.
		/// </summary>
		public static EventGroup<ForumThreadStat> ForumThreadStat;
		
		/// <summary>
		/// Set of events for a ForumStat.
		/// </summary>
		public static EventGroup<ForumStat> ForumStat;
	}
}