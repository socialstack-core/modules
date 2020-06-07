using Microsoft.AspNetCore.Mvc;

namespace Api.Polls
{
    /// <summary>Handles poll response endpoints.</summary>
    [Route("v1/pollresponse")]
	public partial class PollResponseController : AutoController<PollResponse>
    {
    }
}