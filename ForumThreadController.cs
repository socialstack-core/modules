using Microsoft.AspNetCore.Mvc;

namespace Api.ForumThreads
{
    /// <summary>
    /// Handles forum thread endpoints.
    /// </summary>
    [Route("v1/forum/thread")]
	public partial class ForumThreadController : AutoController<ForumThread>
    {
    }
}