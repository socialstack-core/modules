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
	}

}