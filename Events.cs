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
		public static LocaleEventGroup Locale;
		
		/// <summary>
		/// Set of events for a Translation.
		/// </summary>
		public static EventGroup<Translation> Translation;
	}

	/// <summary>
	/// Event group for locales
	/// </summary>
	public partial class LocaleEventGroup : EventGroup<Locale>
	{

		/// <summary>
		/// Locales are needed by the database system before the locale service has even loaded.
		/// This initial set of locales has a special event for the data service to provide it.
		/// </summary>
		public EventHandler<List<Locale>> InitialList;

	}

}
