using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;

namespace Api.StoryAttachments
{

	/// <summary>
	/// Usually an attachment on activity feed entries.
	/// </summary>
	public partial class StoryAttachment : MappingEntity
	{
		/// <summary>
		/// Creator user
		/// </summary>
		public uint UserId;

		/// <summary>
		/// Target URL where this attachment can be found. Can be a ref such as page:1 or module:ComponentName
		/// </summary>
		[DatabaseField(Length = 1000)]
		public string Url;
		
		/// <summary>
		/// Ref for image content in this attachment.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;
		
		/// <summary>
		/// A title for this story attachment.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// A small piece of text which describes what this attachment is.
		/// </summary>
		[DatabaseField(Length = 300)]
		public string Excerpt;
		
	}

}