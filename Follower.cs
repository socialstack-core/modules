using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Followers
{

	/// <summary>
	/// A user following (or subscribed to) some other user.
	/// </summary>
	public partial class Follower : RevisionRow
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public int SubscribedToId;
	}

}