using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Followers
{
	/// <summary>
	/// Used when creating or updating a channel user
	/// </summary>
	public partial class FollowerAutoForm : AutoForm<Follower>
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public int SubscribedToId;
	}

}