using Microsoft.AspNetCore.Mvc;

namespace Api.Notifications
{
    /// <summary>Handles notification endpoints.</summary>
    [Route("v1/notification")]
	public partial class NotificationController : AutoController<Notification>
    {
    }
}