using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;

namespace Api.ProfilePermits
{

	/// <summary>
	/// A user following (or subscribed to) some other user.
	/// </summary>
	public partial class ProfilePermit : RevisionRow
	{
		/// <summary>
		/// The user id this (creator) has permitted to see their profile.
		/// </summary>
		public int PermittedUserId;
	}

}