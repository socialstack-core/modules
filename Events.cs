using Api.SupportTickets;
using Api.Permissions;
using Api.ForumThreads;
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
		/// Just before a new support ticket is created. The given support ticket won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketBeforeCreate;

		/// <summary>
		/// Just after an support ticket has been created. The given support ticket object will now have an ID.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketAfterCreate;

		/// <summary>
		/// Just before an support ticket is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketBeforeDelete;

		/// <summary>
		/// Just after an support ticket has been deleted.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketAfterDelete;

		/// <summary>
		/// Just before updating an support ticket. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketBeforeUpdate;

		/// <summary>
		/// Just after updating an support ticket.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketAfterUpdate;

		/// <summary>
		/// Just after an support ticket was loaded.
		/// </summary>
		public static EventHandler<SupportTicket> SupportTicketAfterLoad;

		/// <summary>
		/// Just before a service loads an supportTicket list.
		/// </summary>
		public static EndpointEventHandler<Filter<SupportTicket>> SupportTicketBeforeList;

		/// <summary>
		/// Just after an supportTicket list was loaded.
		/// </summary>
		public static EventHandler<List<SupportTicket>> SupportTicketAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new support ticket.
		/// </summary>
		public static EndpointEventHandler<SupportTicketAutoForm> SupportTicketCreate;
		/// <summary>
		/// Create a new support ticket (called when the forum thread is created before it).
		/// </summary>
		public static EndpointEventHandler<ForumThread> SupportTicketThreadCreate;
		/// <summary>
		/// Delete an support ticket.
		/// </summary>
		public static EndpointEventHandler<SupportTicket> SupportTicketDelete;
		/// <summary>
		/// Update support ticket metadata.
		/// </summary>
		public static EndpointEventHandler<SupportTicketAutoForm> SupportTicketUpdate;
		/// <summary>
		/// Load support ticket metadata.
		/// </summary>
		public static EndpointEventHandler<SupportTicket> SupportTicketLoad;
		/// <summary>
		/// List support tickets.
		/// </summary>
		public static EndpointEventHandler<Filter<SupportTicket>> SupportTicketList;

		#endregion

	}

}
