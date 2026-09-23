using Microsoft.AspNetCore.Mvc;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum thread endpoints.
    /// </summary>
    [Route("v1/forumthread")]
	public partial class ForumThreadController : AutoController<ForumThread>
    {
    }
}