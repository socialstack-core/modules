using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.SmsMessages
{
	
	/// <summary>
	/// A SmsMessage
	/// </summary>
	public partial class SmsTemplate : VersionedContent<uint>
	{
        /// <summary>
        /// The admin facing title of the template.
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Subject;
		
		/// <summary>
		/// The content of this smsMessage.
		/// </summary>
		public string Key;

		/// <summary>
		/// The canvas for the SMS content. Can use graphs etc to link up the message content. Obviously it needs to only emit text!
		/// </summary>
		public string BodyJson;
		
	}

}