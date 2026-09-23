using Microsoft.AspNetCore.Mvc;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum thread statistic endpoints.
    /// </summary>
    [Route("v1/forumthreadstat")]
	public partial class ForumThreadStatController : AutoController<ForumThreadStat>
    {
    }
}