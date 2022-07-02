using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.PushNotifications
{
    /// <summary>
	/// Handles sending push notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
    public partial interface IPushNotificationService
    {
        /// <summary>
        /// Sends a push notification.
        /// </summary>
		/// <returns>
		/// Returns true if the notification was added to the queue.
		/// </returns>
        bool Send(Context context, PushNotification notification);
    }
}