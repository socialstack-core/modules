using Api.AutoForms;


namespace Api.Quizzes
{
    /// <summary>
    /// Used when creating or updating a quiz answer
    /// </summary>
    public partial class QuizAnswerAutoForm : AutoForm<QuizAnswer>
	{
		/// <summary>
		/// The title of the answer.
		/// </summary>
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
