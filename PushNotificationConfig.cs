using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.PushNotifications
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public class PushNotificationConfig
    {
        /// <summary>
        /// The server key to use in the HTTPS request headers.
        /// </summary>
        public string ServerKey { get; set; }

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
