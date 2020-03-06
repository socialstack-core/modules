using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;


namespace Api.Quizzes
{
	/// <summary>
	/// A quiz. These contain a list of quiz answers.
	/// </summary>
	public partial class QuizAnswer : RevisionRow
	{
		/// <summary>
		/// The answer title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// Optional extra answer content. Can include images etc here.
		/// </summary>
		public string BodyJson;
		
		/// <summary>
		/// The parent question Id.
		/// </summary>
		public int QuestionId;
	}
}