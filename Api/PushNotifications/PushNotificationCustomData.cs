using System;

namespace Api.PushNotifications
{
    /// <summary>
    /// Used when sending custom data in push notifications. Either extend this with another partial, or inherit it.
    /// </summary>
    public partial class PushNotificationCustomData
    {

		/// <summary>
		/// The url of the message.
		/// </summary>
		public string url;

		/// <summary>
		/// The title of the message.
		/// </summary>
		public string title;

		/// <summary>
		/// The body of the message.
		/// </summary>
		public string body;
		
		// /// <summary>
		// /// Category as used by APNS.
		// /// </summary>
		// public string category = "primary";
		
		// /// <summary>
		// /// Click action as used by Android.
		// /// </summary>
		// public string click_action = "primary";
		
	}
}