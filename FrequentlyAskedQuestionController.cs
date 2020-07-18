using Microsoft.AspNetCore.Mvc;

namespace Api.FrequentlyAskedQuestions
{
    /// <summary>Handles frequentlyAskedQuestion endpoints.</summary>
    [Route("v1/frequentlyAskedQuestion")]
	public partial class FrequentlyAskedQuestionController : AutoController<FrequentlyAskedQuestion>
    {
    }
}