using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.Notifications
{
	
	/// <summary>
	/// A Notification
	/// </summary>
	public partial class Notification : UserCreatedContent<uint>, IAmLive
	{
        /// <summary>
        /// The title text of the notification
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// Target URL.
		/// </summary>
		public string Url;
		
		/// <summary>
		/// Set when the user has clicked on the notification or the associated content has marked it as seen.
		/// If null, the notification is "new".
		/// </summary>
		public DateTime? ViewedDateUtc;
		
		/// <summary>
		/// Optional feature image ref
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;
		
	}

}