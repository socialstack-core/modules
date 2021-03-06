using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PrivateChats
{
	
	/// <summary>
	/// A PrivateChatMessage
	/// </summary>
	public partial class PrivateChatMessage : VersionedContent<uint>
	{
		/// <summary>
		/// The private chat this message is in.
		/// </summary>
		public uint PrivateChatId;
		
		/// <summary>
		/// The message text.
		/// </summary>
		[DatabaseField(Length=1000)]
		public string Message;
	}

}