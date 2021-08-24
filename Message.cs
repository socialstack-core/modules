using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.Chats;
using Api.Startup;


namespace Api.Messages
{
	
	/// <summary>
	/// A Message
	/// </summary>
	[ListAs("Messages")]
	[HasVirtualField("CreatorUser", typeof(User), "UserId")]
	[HasVirtualField("Chat", typeof(Chat), "ChatId")]
	public partial class Message : UserCreatedContent<uint>
	{
		/// <summary>
		/// The private chat this message is in.
		/// </summary>
		public uint ChatId;

		/// <summary>
		/// The message text.
		/// </summary>
		[DatabaseField(Length = 1000)]
		public string Text;

	}

}