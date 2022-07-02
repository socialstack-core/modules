using Microsoft.AspNetCore.Mvc;

namespace Api.FeedStories
{
    /// <summary>
    /// Handles feedStory endpoints.
    /// </summary>
    [Route("v1/feed/story")]
	public partial class FeedStoryController : AutoController<FeedStory>
    {
	}
}