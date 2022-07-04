using Api.Matchmakers;
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
		/// Set of events for a match.
		/// </summary>
		public static EventGroup<Match> Match;
		
		/// <summary>
		/// Set of events for a matchmaker.
		/// </summary>
		public static EventGroup<Matchmaker> Matchmaker;
		
		/// <summary>
		/// Set of events for a matchServer.
		/// </summary>
		public static EventGroup<MatchServer> MatchServer;
	}
}