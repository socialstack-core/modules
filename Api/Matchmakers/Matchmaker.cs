using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Matchmakers
{
	
	/// <summary>
	/// A Matchmaker
	/// </summary>
	public partial class Matchmaker : VersionedContent<uint>
	{
		/// <summary>
		/// Server region. Use 0 to indicate no use of regions.
		/// Otherwise, the regions are purely down to a particular project.
		/// E.g. 1=Europe, 2=Asia, 3=NA East, 4=NA West, 5=Oceania, 6=South America, 7=Africa
		/// This indicates which region the matchmaker will exclusively use when selecting a server.
		/// </summary>
		public uint RegionId;

		/// <summary>
		/// True if this matchmaker also should assign a match to a server.
		/// </summary>
		public bool UsesServers;

		/// <summary>
		/// The activity that we're matchmaking. This can be e.g. a game ID, or a game mode ID.
		/// Current assumption is all servers can handle all activities.
		/// </summary>
		public uint ActivityId;

		/// <summary>
		/// True if this is a "sticky" matchmaker. This means if someone has been seen before, they will be matchmade into the same match again.
		/// </summary>
		public bool Sticky;

		/// <summary>
		/// Max users in a single match.
		/// </summary>
		public int MaxMatchSize = 6;
		
		/// <summary>
		/// E.g. if this is a solo queue, teams of 2+ people can't join.
		/// There's no min though - people can play team games solo if they want.
		/// </summary>
		public int MaxTeamSize = 1;
		
		/// <summary>
		/// Matchmaker nice name. E.g. "Public Solos"
		/// </summary>
		[DatabaseField(Length=200)]
		public string Name;
		
		/// <summary>
		/// Maximum queue time, in seconds, before a game will start automatically.
		/// Triggered by a second team joining the match.
		/// </summary>
		public int MaxQueueTime;
		
		/// <summary>
		/// ID of the match that is currently being packed.
		/// </summary>
		public uint CurrentMatchId;
		
		/// <summary>
		/// Number of teams (squads, duo's etc) added to the current match so far.
		/// </summary>
		public int TeamsAdded;
		
		/// <summary>
		/// Number of individual users added to the current match so far.
		/// </summary>
		public int UsersAdded;
		
		/// <summary>
		/// Min number of teams required to trigger match start countdown.
		/// </summary>
		public int MinTeamCount = 2;
		
		/// <summary>
		/// The match start time. Set when a second team joins the match to now + max queue time.
		/// It's overwritten however if a match becomes completely full.
		/// </summary>
		public DateTime? StartTimeUtc;

		/// <summary>
		/// Match creation task, if there is one.
		/// </summary>
		[JsonIgnore]
		public System.Threading.Tasks.Task<Match> MatchCreateTask { get; set; }

		/// <summary>
		/// True if this matchmaker always returns CurrentMatchId.
		/// </summary>
		public bool SameResponseAlways;
	}

}