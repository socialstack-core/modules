using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Matchmaking
{
	
	/// <summary>
	/// A Match
	/// </summary>
	public partial class Match : RevisionRow
	{
		/// <summary>
		/// Server region. Use 0 to indicate no use of regions.
		/// Otherwise, the regions are purely down to a particular project.
		/// E.g. 1=Europe, 2=Asia, 3=NA East, 4=NA West, 5=Oceania, 6=South America, 7=Africa
		/// </summary>
		public int RegionId;
		
		/// <summary>
		/// The activity that this match is for. This can be e.g. a game ID, or a game mode ID.
		/// Current assumption is all servers can handle all activities.
		/// </summary>
		public int ActivityId;
		
		/// <summary>
		/// The server this match is being hosted on.
		/// </summary>
		public int MatchServerId;
		
		/// <summary>
		/// Id of the matchmaker this originated from.
		/// </summary>
		public int MatchmakerId;
		
		/// <summary>
		/// Maximum queue time, in seconds, before a game will start automatically.
		/// Triggered by a second team joining the match.
		/// </summary>
		public int MaxQueueTime;
	}

}