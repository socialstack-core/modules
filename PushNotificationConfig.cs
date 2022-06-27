using Api.Configuration;

namespace Api.PushNotifications
{
	/// <summary>
	/// The config block for push notifications (see admin -> settings)
	/// </summary>
    public class PushNotificationConfig : Config
    {
        /// <summary>
        /// The server key to use in the HTTPS request headers.
        /// </summary>
        public string ServerKey { get; set; }

		/// <summary>
		/// FCM Sender ID (required).
		/// </summary>
		public string SenderId { get; set; }

		/// <summary>
		/// The APNS server key to use for iOS devices.
		/// </summary>
		public string APNSKey { get; set; }

		/// <summary>
		/// APNS key Id
		/// </summary>
		public string APNSKeyId { get; set; }

		/// <summary>
		/// APNS team Id
		/// </summary>
		public string APNSTeamId { get; set; }

		/// <summary>
		/// Bundle id on iOS
		/// </summary>
		public string iOSBundleId { get; set; }
	}
	
}
