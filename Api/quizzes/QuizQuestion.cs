using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;

namespace Api.Quizzes
{
	
	/// <summary>
	/// A quiz question. These contain a list of answers.
	/// </summary>
	public partial class QuizQuestion : RevisionRow
	{
		/// <summary>
		/// The quiz this question is in.
		/// </summary>
		public int QuizId;
		/// <summary>
		/// The question title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		/// <summary>
		/// Optional extra quiz content. Can include images etc here.
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// The page ID that this question appears on.
		/// </summary>
		public int PageId;
	}
	
}