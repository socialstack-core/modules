using Api.Translate;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a Locale.
		/// </summary>
		public static EventGroup<Locale> Locale;
		
		/// <summary>
		/// Set of events for a Translation.
		/// </summary>
		public static EventGroup<Translation> Translation;
	}

}
