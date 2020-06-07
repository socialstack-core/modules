using Api.Polls;
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
		/// Set of events for a poll.
		/// </summary>
		public static EventGroup<Poll> Poll;
		
		/// <summary>
		/// Set of events for a PollAnswer.
		/// </summary>
		public static EventGroup<PollAnswer> PollAnswer;
		
		/// <summary>
		/// Set of events for a PollResponse.
		/// </summary>
		public static EventGroup<PollResponse> PollResponse;
	}
}