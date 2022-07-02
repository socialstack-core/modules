using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Questions
{
	
	/// <summary>
	/// A question. These contain a list of answers.
	/// </summary>
	public partial class Question : RevisionRow, IHaveReactions
	{
		/// <summary>
		/// The board this question is in.
		/// </summary>
		public int QuestionBoardId;
		/// <summary>
		/// The question title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		/// <summary>
		/// The JSON body of the main post. It's JSON because it is a *canvas*. 
		/// This means the reply can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;

		/// <summary>
		/// The page ID that this question appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// If an answer was accepted, this is its ID.
		/// </summary>
		public int? AcceptedAnswerId;

		/// <summary>
		/// Reactions to this question (typically upvote/ downvote).
		/// </summary>
		public List<ReactionCount> Reactions { get; set; }
	}
	
}