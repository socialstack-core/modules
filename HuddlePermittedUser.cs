using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddlePermittedUser
	/// </summary>
	public partial class HuddlePermittedUser : RevisionRow
	{
		/// <summary>
		/// User allowed to a private huddle.
		/// </summary>
		public int PermittedUserId;
		
		/// <summary>
		/// The huddle ID.
		/// </summary>
		public int HuddleId;

		/// <summary>
		/// The permitted user (if there is one - can be null).
		/// </summary>
		public UserProfile PermittedUser { get; set; }
	}

}