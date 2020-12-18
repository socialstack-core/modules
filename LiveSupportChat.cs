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
	public partial class LiveSupportChat : RevisionRow, IAmLive
	{
        /// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// User ID of current support worker responding to the chat.
		/// </summary>
		public int? AssignedToUserId;

		/// <summary>
		/// The full name of the user that in initiated this chat.
		/// </summary>
		public string FullName;
	}

}