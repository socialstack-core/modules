using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.CalendarEvents
{
    /// <summary>
    /// Used when creating or updating an event
    /// </summary>
    public partial class EventAutoForm : AutoForm<Event>
    {
		/// <summary>
		/// The name of the event in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// A short description of the event.
		/// </summary>
		public string Description;

		/// <summary>
		/// The primary ID of the page that this event appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The content of this event.
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
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
