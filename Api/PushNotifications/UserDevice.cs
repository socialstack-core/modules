using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PushNotifications
{
	
	/// <summary>
	/// An UserDevice
	/// </summary>
	public partial class UserDevice : VersionedContent<uint>
	{
		/// <summary>
		/// Usually a firebase key for sending push notifs to this device.
		/// </summary>
		public string NotificationKey;
		
		/// <summary>
		/// Indicates how NotificationKey was obtained and which platform it is for.
		/// </summary>
		public string NotificationType;
	}

}