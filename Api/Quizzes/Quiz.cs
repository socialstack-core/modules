using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;


namespace Api.Quizzes
{
	/// <summary>
	/// A quiz. These contain a list of quiz questions.
	/// </summary>
	public partial class Quiz : RevisionRow
	{
		/// <summary>
		/// The quiz title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// The description of the quiz
		/// </summary>
		public string Description;
		
		/// <summary>
		/// Optional extra quiz content. Can include images etc here.
		/// </summary>
		public string BodyJson;
		
		/// <summary>
		/// The page ID that this quiz appears on.
		/// </summary>
		public int PageId;
		
		/// <summary>
		/// The page ID that this quizzes questions appear on.
		/// </summary>
		public int QuestionPageId;
	}
}