using Api.AutoForms;


namespace Api.Quizzes
{
    /// <summary>
    /// Used when creating or updating a quiz
    /// </summary>
    public partial class QuizAutoForm : AutoForm<Quiz>
	{
		/// <summary>
		/// The title of the quiz.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// The description of the quiz
		/// </summary>
		public string Description;

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
