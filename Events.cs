using Api.Permissions;
using System.Collections.Generic;
using Api.Translate;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
    {

		#region Service events

		/// <summary>
		/// Just before a new translation is created. The given translation won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Translation> TranslationBeforeCreate;

		/// <summary>
		/// Just after an translation has been created. The given translation object will now have an ID.
		/// </summary>
		public static EventHandler<Translation> TranslationAfterCreate;

		/// <summary>
		/// Just before an translation is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Translation> TranslationBeforeDelete;

		/// <summary>
		/// Just after an translation has been deleted.
		/// </summary>
		public static EventHandler<Translation> TranslationAfterDelete;

		/// <summary>
		/// Just before updating an translation. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Translation> TranslationBeforeUpdate;

		/// <summary>
		/// Just after updating an translation.
		/// </summary>
		public static EventHandler<Translation> TranslationAfterUpdate;

		/// <summary>
		/// Just after an translation was loaded.
		/// </summary>
		public static EventHandler<Translation> TranslationAfterLoad;

		/// <summary>
		/// Just before a service loads an translation list.
		/// </summary>
		public static EventHandler<Filter<Translation>> TranslationBeforeList;

		/// <summary>
		/// Just after an translation list was loaded.
		/// </summary>
		public static EventHandler<List<Translation>> TranslationAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new translation.
		/// </summary>
		public static EndpointEventHandler<TranslationAutoForm> TranslationCreate;
		/// <summary>
		/// Delete an translation.
		/// </summary>
		public static EndpointEventHandler<Translation> TranslationDelete;
		/// <summary>
		/// Update translation metadata.
		/// </summary>
		public static EndpointEventHandler<TranslationAutoForm> TranslationUpdate;
		/// <summary>
		/// Load translation metadata.
		/// </summary>
		public static EndpointEventHandler<Translation> TranslationLoad;
		/// <summary>
		/// List translations.
		/// </summary>
		public static EndpointEventHandler<Filter<Translation>> TranslationList;

		#endregion

	}

}
