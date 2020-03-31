using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Answers
{

	/// <summary>
	/// A question answer.
	/// </summary>
	public partial class Answer : RevisionRow, IHaveReactions
	{
		/// <summary>
		/// The question this answer is part of.
		/// </summary>
		public int QuestionId;
		/// <summary>
		/// The board this answer is in.
		/// </summary>
		public int QuestionBoardId;
		
		/// <summary>
		/// If answers can nest, this is 
		/// used to indicate the sort order of the question's answers as a whole.
		/// </summary>
		[DatabaseField(Length = 20)]
		public byte[] Order;
		/// <summary>
		/// The JSON body of this answer. It's JSON because it is a *canvas*. 
		/// This means the answer can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;
		/// <summary>
		/// The creator user of the question that this answer is in. 
		/// Stored in the answers as it doesn't change and improves the efficiency of the permission system.
		/// </summary>
		public int QuestionCreatorId;

		/// <summary>
		/// True if this is an accepted answer. Must also have the same ID as the Question.AcceptedAnswerId.
		/// </summary>
		public bool IsAcceptedAnswer;

		/// <summary>
		/// Reactions to this answer (typically upvote/ downvote).
		/// </summary>
		public List<ReactionCount> Reactions { get; set; }
	}

}