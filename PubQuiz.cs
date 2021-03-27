using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PubQuizzes
{
	
	/// <summary>
	/// A PubQuiz
	/// </summary>
	public partial class PubQuiz : VersionedContent<int>
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
		[DatabaseField(Length = 400)]
		[Localized]
		public string Description;
	}

}