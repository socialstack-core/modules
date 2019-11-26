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
		/// If successful, the push notification is returned with a successful state.
		/// </returns>
        Task<PushNotification> Send(Context context, PushNotification notification);
    }
}