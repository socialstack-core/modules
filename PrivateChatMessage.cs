using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PrivateChats
{
	
	/// <summary>
	/// A PrivateChatMessage
	/// </summary>
	public partial class PrivateChatMessage : RevisionRow
	{
		/// <summary>
		/// The private chat this message is in.
		/// </summary>
		public int PrivateChatId;
		
		/// <summary>
		/// Private chat can be between two entities on the site. Usually that's a user->user, but can 
		/// also be e.g. user->company or company->company or company->group etc.
		/// ContentId of the original sender.
		/// </summary>
		public int SourceContentId;
		
		/// <summary>
		/// Private chat can be between two entities on the site. Usually that's a user->user, but can 
		/// also be e.g. user->company or company->company or company->group etc.
		/// ContentType of the original sender.
		/// </summary>
		public int SourceContentType;
		
		/// <summary>
		/// Private chat can be between two entities on the site. Usually that's a user->user, but can 
		/// also be e.g. user->company or company->company or company->group etc.
		/// ContentType of the original recipient.
		/// </summary>
		public int TargetContentType;
		
		/// <summary>
		/// Private chat can be between two entities on the site. Usually that's a user->user, but can 
		/// also be e.g. user->company or company->company or company->group etc.
		/// ContentId of the original recipient.
		/// </summary>
		public int TargetContentId;
		
		/// <summary>
		/// The message text.
		/// </summary>
		[DatabaseField(Length=1000)]
		public string Message;
	}

}