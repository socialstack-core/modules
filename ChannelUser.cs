using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.ChannelUsers
{

	/// <summary>
	/// A user within a particular channel.
	/// </summary>
	public partial class ChannelUser : RevisionRow
	{
		/// <summary>
		/// The channel that this user is in.
		/// </summary>
		public int ChannelId;
	}

}