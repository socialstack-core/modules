using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Matchmaking
{
	
	/// <summary>
	/// A MatchServer
	/// </summary>
	public partial class MatchServer : RevisionRow
	{
		/// <summary>
		/// Server region. Use 0 to indicate no use of regions.
		/// Otherwise, the regions are purely down to a particular project.
		/// E.g. 1=Europe, 2=Asia, 3=NA East, 4=NA West, 5=Oceania, 6=South America
		/// </summary>
		public int RegionId;
		
		/// <summary>
		/// Server URL/ Address
		/// </summary>
		public string Address;

		/// <summary>
		/// Bolt server ID. Used to message the server directly from a web node.
		/// </summary>
		public int ServerId;
	}

}