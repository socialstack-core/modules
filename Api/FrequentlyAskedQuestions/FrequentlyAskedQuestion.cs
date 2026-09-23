using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.FrequentlyAskedQuestions
{
	
	/// <summary>
	/// A FrequentlyAskedQuestion
	/// </summary>
	public partial class FrequentlyAskedQuestion : VersionedContent<uint>
	{
        /// <summary>
        /// The question people ask.
        /// </summary>
		public Localized<string> Question;

		/// <summary>
		/// The answer in HTML.
		/// </summary>
		public Localized<string> AnswerHtml;
	}

}