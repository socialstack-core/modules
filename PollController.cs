using Microsoft.AspNetCore.Mvc;

namespace Api.Polls
{
    /// <summary>Handles poll endpoints.</summary>
    [Route("v1/poll")]
	public partial class PollController : AutoController<Poll>
    {
    }
}