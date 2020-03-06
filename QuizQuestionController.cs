using Microsoft.AspNetCore.Mvc;


namespace Api.Quizzes
{
    /// <summary>
    /// Handles quiz question endpoints.
    /// </summary>

    [Route("v1/quizquestion")]
	public partial class QuizQuestionController : AutoController<QuizQuestion, QuizQuestionAutoForm>
    {
    }
}