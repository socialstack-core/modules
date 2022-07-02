using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;
using Api.StoryAttachments;


namespace Api.ChannelMessages
{

	/// <summary>
	/// A message within a particular channel.
	/// </summary>
	public partial class ChannelMessage : RevisionRow, IHaveStoryAttachments
	{
		/// <summary>
		/// The channel this is in.
		/// </summary>
		public int ChannelId;
		
		/// <summary>
		/// The JSON body of this message. It's JSON because it is a *canvas*. 
		/// This means the answer can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;

		/// <summary>
		/// Attachments - websites, images.
		/// </summary>
		public List<StoryAttachment> Attachments { get; set; }
	}

}