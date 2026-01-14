using Api.AuditEvents;
using Api.Payments;
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
		/// Set of events for an auditEvent.
		/// </summary>
		public static AuditEventEventGroup AuditEvent;
	}

	/// <summary>
	/// Specialised event group for the Product event type.
	/// </summary>
	public partial class AuditEventEventGroup : EventGroup<AuditEvent>
	{

		/// <summary>
		/// Called when a content type with revisions is attempting to register with the audit log.
		/// Block it from doing so by returning null.
		/// </summary>
		public EventHandler<AuditEventType> OnRegisterType;

		/// <summary>
		/// Called when running a search for audit log entries.
		/// </summary>
		public EventHandler<AuditLogSearch> BeforeSearch;

		/// <summary>
		/// Called when running a search for audit log entries.
		/// </summary>
		public EventHandler<AuditLogSearch> Search;
	}
}