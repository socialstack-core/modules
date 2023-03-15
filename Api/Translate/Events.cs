using Api.Translate;
using Api.Permissions;
using System.Collections.Generic;
using Api.Startup;

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
		/// Called when the set locale endpoint is used.
		/// </summary>
		public EventHandler<uint> SetLocale;
		
		/// <summary>
		/// Locales are needed by the database system before the locale service has even loaded.
		/// This initial set of locales has a special event for the data service to provide it.
		/// </summary>
		public EventHandler<List<Locale>> InitialList;

		/// <summary>
		/// Called when a particular field is being mapped to a .pot text format
		/// </summary>
		public EventHandler<object, ContentField, TranslationServiceConfig> PotFieldValue;

	}

    public partial class EventGroup<T, ID>
    {
		/// <summary>
		/// Called when a list.pot is being generated
		/// </summary>
        public EndpointEventHandler<Filter<T, ID>> EndpointStartPotList;

    }

}
