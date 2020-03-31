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
		/// <summary>
		/// Set of events for a SupportTicket.
		/// </summary>
		public static EventGroup<SupportTicket> SupportTicket;
	}

}
