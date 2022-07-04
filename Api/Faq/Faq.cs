using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Faqs
{
	
	/// <summary>
	/// A Frequently asked question with its answer.
	/// </summary>
	public partial class Faq : RevisionRow
	{
		/// <summary>
		/// The question being asked in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Question;

		/// <summary>
		/// The Name of the Question (essentially, the question.) 
		/// </summary>
		public string Name
		{
			get { return Question; }
		}

		/// <summary>
		/// The primary ID of the page that this faq appears on.
		/// </summary>
		public int PageId;

        /// <summary>
        /// The answer to the question.
        /// </summary>
		[Localized]
		public string AnswerJson;

		/// <summary>
		/// The priority level of the FAQ. The higher the value, the higher 
		/// </summary>
		public int Priority;
	}
	
}