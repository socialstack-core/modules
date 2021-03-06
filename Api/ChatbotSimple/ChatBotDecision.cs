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
		/// Optional ReplyToOverrideId that overwrites the reply to value with the id of another decision. 
		/// </summary>
		public int? ReplyToOverrideId;
		
		/// <summary>
		/// Message type. 0 = User free text response, 1 = multiselect answers
		/// </summary>
		[Module("Admin/ChatBotSimple/MessageTypeSelect")]
		public int MessageType;

		/// <summary>
		/// Optional specific message provided by the user in the context of the in-reply-to message. Case insensitive.
		/// </summary>
		[Localized]
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
		[Localized]
		public string PayloadJson;

		/// <summary>
		/// AlsoSend is the message to also send in response after the inital response. 
		/// </summary>
		public int? AlsoSend;

		/// <summary>
		/// If the mode is set, this indicates its the first message for a given mode.
		/// </summary>
		public int? Mode;

		/// <summary>
		/// The start mode for the message. i.e. is this message starting a meeting appointment or live chat?
		/// 0 (none)
		/// 1 (Live operator)
		/// 2 (Meeting)
		/// 3 (Expert question)
		/// </summary>
		public int StartMode; 
	}

}