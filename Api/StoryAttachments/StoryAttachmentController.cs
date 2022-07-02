using Microsoft.AspNetCore.Mvc;

namespace Api.StoryAttachments
{
    /// <summary>
    /// Handles story attachment endpoints. These attachments are e.g. images attached to a feed story or a message in a chat channel.
    /// </summary>
    [Route("v1/storyattachment")]
	public partial class StoryAttachmentController : AutoController<StoryAttachment>
    {
	}
}
