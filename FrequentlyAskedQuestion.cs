using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.Tags;
using System.Collections.Generic;

namespace Api.FrequentlyAskedQuestions
{
	
	/// <summary>
	/// A FrequentlyAskedQuestion
	/// </summary>
	public partial class FrequentlyAskedQuestion : RevisionRow, IHaveTags
	{
        /// <summary>
        /// The question people ask.
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Question;

		/// <summary>
		/// The answer.
		/// </summary>
		[Localized]
		public string AnswerJson;

		/// <summary>
		/// Tags
		/// </summary>
		public List<Tag> Tags { get; set; }
	}

}