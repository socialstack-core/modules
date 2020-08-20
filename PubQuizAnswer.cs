using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PubQuizzes
{
	
	/// <summary>
	/// A PubQuizAnswer
	/// </summary>
	public partial class PubQuizAnswer : RevisionRow
	{
		/// <summary>
		/// The answer (a canvas so it can e.g. include images)
		/// </summary>
		[DatabaseField(Length=400)]
		public string AnswerJson;
		
		/// <summary>
		/// The quiz ID.
		/// </summary>
		public int PubQuizId;
		
		/// <summary>
		/// The question ID.
		/// </summary>
		public int PubQuizQuestionId;
		
		/// <summary>
		/// True if this is the correct answer.
		/// </summary>
		public bool IsCorrect;
	}

}