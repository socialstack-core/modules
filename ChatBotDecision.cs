using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.ChatBotSimple
{
	
	/// <summary>
	/// A ChatBotDecision
	/// </summary>
	public partial class ChatBotDecision : RevisionRow
	{
		
		/// <summary>
		/// Optional decision ID that the user is replying to that this decision will apply to.
		/// </summary>
		public int InReplyTo;
		
		/// <summary>
		/// Message type. 0 = User free text response, 1 = multiselect answers
		/// </summary>
		[Module("Admin/ChatBotSimple/MessageTypeSelect")]
		public int MessageType;
		
		/// <summary>
		/// Optional specific message provided by the user in the context of the in-reply-to message. Case insensitive.
		/// </summary>
		public string AnswerProvided;
		
        /// <summary>
        /// The main message the chatbot sends.
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string MessageText;
		
		/// <summary>
		/// Payload (Canvas JSON) for more complex message types.
		/// </summary>
		public string PayloadJson;
		
	}

}