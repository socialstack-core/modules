using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddlePresence
	/// </summary>
	public partial class HuddlePresence : UserCreatedContent<uint>
	{
		
		/// <summary>
		/// Assigned huddle server ID.
		/// </summary>
		public uint HuddleId;

		/// <summary>
		/// The temporary ID assigned by the huddle server to indicate which peer this is.
		/// This allows multiple instances of the same user in a particular meeting.
		/// </summary>
		public int PeerId;

		/// <summary>
		/// The ID of the huddle server that the peerId is related to.
		/// </summary>
		public uint HuddleServerId;
		
	}

}