using Microsoft.AspNetCore.Mvc;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum statistic endpoints.
    /// </summary>
    [Route("v1/forumstat")]
	public partial class ForumStatController : AutoController<ForumStat>
    {
    }
}