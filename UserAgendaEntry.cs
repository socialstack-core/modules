using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.UserAgendaEntries
{
	
	/// <summary>
	/// An UserAgendaEntry
	/// </summary>
	public partial class UserAgendaEntry : VersionedContent<int>
	{
		/// <summary>
		/// The type of content that this agenda entry is for. Video, meeting etc.
		/// </summary>
		public int ContentTypeId;
		
		/// <summary>
		/// The ID of the content that this agenda entry is for. Video ID, meeting ID etc.
		/// </summary>
		public int ContentId;
	
		/// <summary>
		/// Start time UTC.
		/// </summary>
		public DateTime StartUtc;
		
		/// <summary>
		/// End time UTC.
		/// </summary>
		public DateTime EndUtc;
		
		/// <summary>
		/// The content that this agenda entry is for.
		/// </summary>
		public object Content {get; set;}
	}

}