using System;

namespace Api.PushNotifications
{
    /// <summary>
    /// Used when sending push notifications.
    /// </summary>
    public partial class PushNotification
    {
		/// <summary>
        /// The body of the push notification.
        /// </summary>
        public string Body;
		
		/// <summary>
        /// The title of the push notification.
        /// </summary>
        public string Title;
		
		/// <summary>
        /// The device key for the target device.
        /// </summary>
        public string TargetDevice;

		/// <summary>
		/// The target device type. "apns", "web" or firebase (other).
		/// </summary>
		public string TargetDeviceType;

		/// <summary>
		/// This is set to true if the notification was sent successfully.
		/// </summary>
		public bool Successful = false;

		/// <summary>
		/// Optional target URL to open.
		/// </summary>
		public string Url;

		/// <summary>
		/// Any additional data to send with this notification. Optional.
		/// </summary>
		public PushNotificationCustomData CustomData;
	}
}