using System;
using System.Collections.Generic;
using Api.Database;
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
		public int? ConnectedToId;
		
		/// <summary>
		/// Close friend, friend, aquaintance, mother, father, spouse etc.
		/// </summary>
		public int ConnectionTypeId;
		
		/// <summary>
		/// The recipient's email address. 
		/// </summary>
		public string? Email;

		/// <summary>
		/// The time that this connection request was accepted, meaning this a valid connection.
		/// </summary>
		public DateTime? AcceptedUtc;

		/// <summary>
		/// The time that this friend connection was declined, meaning that the target user denied the request.
		/// </summary>
		public DateTime? DeclinedUtc;

		/// <summary>
		/// Property containing the connected to User.
		/// </summary>
		public UserProfile ConnectedToUser {
			get; set;
		}
	}

}