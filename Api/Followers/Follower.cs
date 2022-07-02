using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;

namespace Api.Followers
{

	/// <summary>
	/// A user following (or subscribed to) some other user.
	/// </summary>
	public partial class Follower : VersionedContent<uint>
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public uint SubscribedToId;
	}

}