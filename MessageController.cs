using Microsoft.AspNetCore.Mvc;

namespace Api.Messages
{
    /// <summary>Handles message endpoints.</summary>
    [Route("v1/message")]
	public partial class MessageController : AutoController<Message>
    {
    }
}