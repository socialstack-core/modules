using Api.AutoForms;


namespace Api.Quizzes
{
    /// <summary>
    /// Used when creating or updating a quiz question
    /// </summary>
    public partial class QuizQuestionAutoForm : AutoForm<QuizQuestion>
	{
		/// <summary>
		/// The ID of the board that the quiz question will be in.
		/// </summary>
		public int QuizQuestionBoardId;

		/// <summary>
		/// The title of the quiz question.
		/// </summary>
		public string Title;

		/// <summary>
		/// The canvas JSON for the quiz question. If you just want raw text/ html, use {"content": "The text/ html here"}.
		/// It's a full canvas so the quizQuestion can support embedded media and powerful formatting.
		/// </summary>
		public string BodyJson;
    }
}
