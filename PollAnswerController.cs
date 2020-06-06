using Microsoft.AspNetCore.Mvc;

namespace Api.Polls
{
    /// <summary>Handles poll answer endpoints.</summary>
    [Route("v1/pollanswer")]
	public partial class PollAnswerController : AutoController<PollAnswer>
    {
    }
}