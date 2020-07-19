using Microsoft.AspNetCore.Mvc;

namespace Api.PrivateChats
{
    /// <summary>Handles privateChat endpoints.</summary>
    [Route("v1/privateChat")]
	public partial class PrivateChatController : AutoController<PrivateChat>
    {
    }
}