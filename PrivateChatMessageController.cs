using Microsoft.AspNetCore.Mvc;

namespace Api.PrivateChats
{
    /// <summary>Handles privateChatMessage endpoints.</summary>
    [Route("v1/privateChatMessage")]
	public partial class PrivateChatMessageController : AutoController<PrivateChatMessage>
    {
    }
}