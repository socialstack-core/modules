using Api.LegalTexts;
using Api.Permissions;
using System.Collections.Generic;
using Api.TemporaryGuests;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a legalText.
		/// </summary>
		public static EventGroup<TemporaryGuest> TemporaryGuest;
	}
}