using Api.IfAThenB;
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

		#region Service events

		/// <summary>
		/// Just before a new a then b is created. The given a then b won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<AThenB> AThenBBeforeCreate;

		/// <summary>
		/// Just after an a then b has been created. The given a then b object will now have an ID.
		/// </summary>
		public static EventHandler<AThenB> AThenBAfterCreate;

		/// <summary>
		/// Just before an a then b is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<AThenB> AThenBBeforeDelete;

		/// <summary>
		/// Just after an a then b has been deleted.
		/// </summary>
		public static EventHandler<AThenB> AThenBAfterDelete;

		/// <summary>
		/// Just before updating an a then b. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<AThenB> AThenBBeforeUpdate;

		/// <summary>
		/// Just after updating an a then b.
		/// </summary>
		public static EventHandler<AThenB> AThenBAfterUpdate;

		/// <summary>
		/// Just after an a then b was loaded.
		/// </summary>
		public static EventHandler<AThenB> AThenBAfterLoad;

		/// <summary>
		/// Just before a service loads an a then b list.
		/// </summary>
		public static EventHandler<Filter<AThenB>> AThenBBeforeList;

		/// <summary>
		/// Just after an a then b list was loaded.
		/// </summary>
		public static EventHandler<List<AThenB>> AThenBAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new a then b.
		/// </summary>
		public static EndpointEventHandler<AThenBAutoForm> AThenBCreate;
		/// <summary>
		/// Delete an a then b.
		/// </summary>
		public static EndpointEventHandler<AThenB> AThenBDelete;
		/// <summary>
		/// Update a then b metadata.
		/// </summary>
		public static EndpointEventHandler<AThenBAutoForm> AThenBUpdate;
		/// <summary>
		/// Load a then b metadata.
		/// </summary>
		public static EndpointEventHandler<AThenB> AThenBLoad;
		/// <summary>
		/// List a then bs.
		/// </summary>
		public static EndpointEventHandler<Filter<AThenB>> AThenBList;

		#endregion

	}

}
