using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.LiveSupportChats
{
	
	/// <summary>
	/// A LiveSupportMessage
	/// </summary>
	public partial class LiveSupportMessage : RevisionRow, IAmLive
	{
		/// <summary>
		/// The chat this message is in.
		/// </summary>
		public int LiveSupportChatId;
		
		/// <summary>
		/// The message text.
		/// </summary>
		public string Message;
		
		/// <summary>
		/// Indicates if this is a special message, e.g. it contains custom payload such as asking the user a question.
		/// </summary>
		public int MessageType;
		
		/// <summary>
		/// Custom JSON formatted payload.
		/// </summary>
		public string PayloadJson;
		
		/// <summary>
		/// True if message is from the support end.
		/// </summary>
		public bool FromSupport;

		/// <summary>
		/// This is used to carry an important payload that is what we really want, but not what we can render i.e. dates- the utc would look too nasty as a message so we hide and have the message as clean content.
		/// </summary>
		public DateTime? HiddenDatePayload;
	}

}