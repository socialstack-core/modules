using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;
using Api.StoryAttachments;


namespace Api.FeedStories
{

	/// <summary>
	/// A particular feed story.
	/// </summary>
	public partial class FeedStory : RevisionRow, IHaveStoryAttachments
	{
		/// <summary>
		/// Text of this story.
		/// </summary>
		public string BodyJson;
		
		/// <summary>
		/// This stories ranking.
		/// </summary>
		public int Ranking;
		
		/// <summary>
		/// Attachments - websites, images.
		/// </summary>
		public List<StoryAttachment> Attachments {get; set;}
	}

}