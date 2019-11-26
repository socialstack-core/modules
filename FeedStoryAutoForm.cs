using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.FeedStories
{
	/// <summary>
	/// Used when creating or updating a feed story
	/// </summary>
	public partial class FeedStoryAutoForm : AutoForm<FeedStory>
	{
		/// <summary>
		/// Text of this story.
		/// </summary>
		public string BodyJson;
		
		/// <summary>
		/// This stories ranking.
		/// </summary>
		public int Ranking;
		
	}

}