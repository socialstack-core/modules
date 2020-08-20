using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Presence
{
	
	/// <summary>
	/// A PresenceRecord
	/// </summary>
	public partial class PresenceRecord : DatabaseRow
	{
		/// <summary>
		/// E.g. "Page".
		/// </summary>
		[DatabaseField(Length=20)]
		public string EventName;
		
		/// <summary>
		/// If it's a page record, this is the content type ID of Page.
		/// </summary>
		public int ContentTypeId;
		
		/// <summary>
		/// Relevant content ID. E.g. this may be a page ID.
		/// </summary>
		public int ContentId;
		
		/// <summary>
		/// The meta for this presence record.
		/// E.g. the actual URL that was loaded, if it's for a page.
		/// </summary>
		[DatabaseField(Length=300)]
		public string MetaJson;
		
		/// <summary>
		/// The ID of the user this record was created for. Can be 0.
		/// </summary>
		public int UserId;
		
		/// <summary>
		/// The time this record occurred.
		/// </summary>
		public DateTime CreatedUtc;
		
	}

}