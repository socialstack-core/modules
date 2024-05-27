using Microsoft.AspNetCore.Mvc;

namespace Api.PushNotifications
{
    /// <summary>Handles userDevice endpoints.</summary>
    [Route("v1/userDevice")]
	public partial class UserDeviceController : AutoController<UserDevice>
    {
    }
}