using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PubQuizzes
{
	
	/// <summary>
	/// A PubQuiz
	/// </summary>
	public partial class PubQuiz : RevisionRow
	{
        /// <summary>
        /// The name of the PubQuiz
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Title;

		/// <summary>
		/// The name of the PubQuiz
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Description;
	}

}