using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Connections
{

	/// <summary>
	/// A user following (or subscribed to) some other user.
	/// </summary>
	public partial class Connection : RevisionRow
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public int ConnectedToId;
		
		/// <summary>
		/// Close friend, friend, aquaintance, mother, father, spouse etc.
		/// </summary>
		public int ConnectionTypeId;
		
	}

}