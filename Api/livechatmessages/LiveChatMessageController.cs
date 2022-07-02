using Microsoft.AspNetCore.Mvc;

namespace Api.LiveChats
{
    /// <summary>
    /// Handles live chat message endpoints.
    /// </summary>

    [Route("v1/livechatmessage")]
	public partial class LiveChatMessageController : AutoController<LiveChatMessage>
    {
    }
}