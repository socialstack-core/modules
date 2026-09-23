using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.CalendarEvents
{
	
	/// <summary>
	/// A CalendarEvent
	/// </summary>
	public partial class CalendarEvent : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the event in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public Localized<string> Name;

		/// <summary>
		/// A short description of the event.
		/// </summary>
		[DatabaseField(Length = 200)]
		public Localized<string> Description;

		/// <summary>
		/// The primary ID of the page that this event appears on.
		/// </summary>
		public int PageId;
		
		/// <summary>
		/// The content of this event.
		/// </summary>
		public Localized<string> BodyJson;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;

        /// <summary>
        /// The startUtc for the event.
        /// </summary>
        public DateTime StartUtc;

        /// <summary>
        /// The endUtc for the event.
        /// </summary>
        public DateTime EndUtc;
	}

}