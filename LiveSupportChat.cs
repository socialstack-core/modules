using System;
using System.Collections.Generic;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.LiveSupportChats
{
	
	/// <summary>
	/// A LiveSupportChat
	/// </summary>
	public partial class LiveSupportChat : RevisionRow, IHaveUserRestrictions, IAmLive
	{
        /// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// Access to a chat is restricted to specific people.
		/// </summary>
		public bool UserRestrictionsActive => true;

		/// <summary>
		/// Users are allowed to see other users who are in the chat with them.
		/// </summary>
		public bool PermittedUsersListVisible => true;

		/// <summary>
		/// Users in a chat
		/// </summary>
		public List<PermittedContent> PermittedUsers { get; set; }
		
		/// <summary>
		/// User ID of current support worker responding to the chat.
		/// </summary>
		public int? AssignedToUserId;
	}

}