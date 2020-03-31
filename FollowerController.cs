using Microsoft.AspNetCore.Mvc;

namespace Api.Followers
{
    /// <summary>
    /// Handles follower endpoints.
    /// </summary>
    [Route("v1/follower")]
	public partial class FollowerController : AutoController<Follower>
    {
	}
}