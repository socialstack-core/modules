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
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// Set when loading this private chat.
		/// </summary>
		public object Target {get; set;}

		/// <summary>
		/// Set when loading this private chat.
		/// </summary>
		public object Source { get; set; }
		
		/// <summary>
		/// Optional first message that can be provided when starting a new chat.
		/// </summary>
		public string Message {get; set;}
	}

}