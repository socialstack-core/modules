using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PrivateChats
{
	
	/// <summary>
	/// A PrivateChat
	/// </summary>
	public partial class PrivateChat : RevisionRow
	{
		/// <summary>
		/// CreatorUserId is chatting with WithUserId.
		/// </summary>
		public int WithUserId;
		
		/// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// Set when loading this private chat.
		/// </summary>
		public UserProfile WithUser {get; set;}
	}

}