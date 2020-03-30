using Microsoft.AspNetCore.Mvc;

namespace Api.Reactions
{
    /// <summary>
    /// Handles reaction endpoints (liking, disliking, upvoting, downvoting, love hearting etc).
    /// </summary>

    [Route("v1/reaction")]
	public partial class ReactionController : AutoController<Reaction>
    {
    }

}
