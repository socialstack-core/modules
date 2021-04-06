using System;
using System.Collections.Generic;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PubQuizzes
{
	
	/// <summary>
	/// A PubQuizQuestion
	/// </summary>
	public partial class PubQuizQuestion : VersionedContent<uint>
	{
		/// <summary>
		/// The quiz ID.
		/// </summary>
		public int PubQuizId;
		
		/// <summary>
		/// The question (as a canvas, so it can include e.g. images).
		/// </summary>
		[DatabaseField(Length=400)]
		public string QuestionJson;

		/// <summary>
		/// The answers of this question.
		/// </summary>
		public List<PubQuizAnswer> Answers { get; set; }
	}

}