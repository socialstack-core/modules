namespace Api.Users{
	
	public partial class User{
		
		/// <summary>
		/// The ID of the huddle this user last joined.
		/// Note that they can potentially be in more than one at a time.
		/// </summary>
		public uint LastJoinedHuddleId;
		
		/// <summary>
		/// True if this user is in a meeting.
		/// </summary>
		public bool InMeeting;
		
	}
	
	public partial class UserProfile{
		
		/// <summary>
		/// True if this user is in a meeting.
		/// </summary>
		public bool InMeeting;
		
	}
	
}