using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;
using Api.Startup;

namespace Api.Connections
{

	/// <summary>
	/// A user following (or subscribed to) some other user.
	/// </summary>
	[HasVirtualField("ConnectedToUser", typeof(User), "ConnectedToId")]
	[HasVirtualField("CreatorUser", typeof(User), "UserId")]
	public partial class Connection : UserCreatedContent<uint>
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public uint ConnectedToId;
		
		/// <summary>
		/// Close friend, friend, aquaintance, mother, father, spouse etc.
		/// </summary>
		public int ConnectionTypeId;
		
		/// <summary>
		/// The recipient's email address. 
		/// </summary>
		public string Email;

		/// <summary>
		/// The time that this connection request was accepted, meaning this a valid connection.
		/// </summary>
		public DateTime? AcceptedUtc;

		/// <summary>
		/// The time that this friend connection was declined, meaning that the target user denied the request.
		/// </summary>
		public DateTime? DeclinedUtc;
	}

}