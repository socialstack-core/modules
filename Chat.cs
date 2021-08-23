using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;
using Api.Startup;
using Api.Messages;

namespace Api.Chats
{
	
	/// <summary>
	/// A Chat
	/// </summary>
	[HasVirtualField("LastMessage", typeof(Message), "LastMessageId")]
	public partial class Chat : VersionedContent<uint>
	{
		/// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the lastEditedUtc date to change.
		/// </summary>
		public int MessageCount;

		/// <summary>
		/// The Id of the last Message sent in this chat.
		/// </summary>
		public uint LastMessageId;
		
	}

}