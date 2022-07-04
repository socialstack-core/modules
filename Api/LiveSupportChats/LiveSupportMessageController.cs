using Microsoft.AspNetCore.Mvc;

namespace Api.LiveSupportChats
{
    /// <summary>Handles liveSupportMessage endpoints.</summary>
    [Route("v1/liveSupportMessage")]
	public partial class LiveSupportMessageController : AutoController<LiveSupportMessage>
    {
    }
}