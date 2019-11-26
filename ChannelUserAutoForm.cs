using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.ChannelUsers
{
	/// <summary>
	/// Used when creating or updating a channel user
	/// </summary>
	public partial class ChannelUserAutoForm : AutoForm<ChannelUser>
	{
		/// <summary>
		/// The channel this user should be added to.
		/// </summary>
		public int ChannelId;
		
		/// <summary>
		/// The user in the channel.
		/// </summary>
		public int UserId;
	}

}