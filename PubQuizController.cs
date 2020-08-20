using Microsoft.AspNetCore.Mvc;

namespace Api.PubQuizzes
{
    /// <summary>Handles PubQuiz endpoints.</summary>
    [Route("v1/pubQuiz")]
	public partial class PubQuizzController : AutoController<PubQuiz>
    {
    }
}