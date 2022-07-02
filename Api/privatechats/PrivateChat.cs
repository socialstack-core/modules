using System;
using System.Collections.Generic;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;
using Api.WebSockets;

namespace Api.PrivateChats
{
	
	/// <summary>
	/// A PrivateChat
	/// </summary>
	public partial class PrivateChat : VersionedContent<uint>
	{
		/// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// Optional first message that can be provided when starting a new chat.
		/// </summary>
		public string Message {get; set;}
		
		/// <summary>
		/// Access to a private chat is restricted to specific people.
		/// </summary>
		public bool UserRestrictionsActive => true;

		/// <summary>
		/// Users are allowed to see other users who are in the chat with them.
		/// </summary>
		public bool PermittedUsersListVisible => true;
	}

}