using Microsoft.AspNetCore.Mvc;


namespace Api.Quizzes
{
    /// <summary>
    /// Handles quiz endpoints.
    /// </summary>

    [Route("v1/quiz")]
	public partial class QuizController : AutoController<Quiz, QuizAutoForm>
    {
    }
}