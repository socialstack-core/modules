using Microsoft.AspNetCore.Mvc;

namespace Api.PubQuizzes
{
    /// <summary>Handles pubQuizQuestion endpoints.</summary>
    [Route("v1/pubQuizQuestion")]
	public partial class PubQuizQuestionController : AutoController<PubQuizQuestion>
    {
    }
}