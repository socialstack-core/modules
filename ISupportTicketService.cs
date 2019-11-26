using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.SupportTickets
{
	/// <summary>
	/// Handles support tickets.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ISupportTicketService
	{
		/// <summary>
		/// Deletes a ticket by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int ticketId, bool deleteReplies = true);

		/// <summary>
		/// Gets a single ticket by its ID.
		/// </summary>
		Task<SupportTicket> Get(Context context, int ticketId);

		/// <summary>
		/// Creates a new ticket.
		/// </summary>
		Task<SupportTicket> Create(Context context, SupportTicket ticket);

		/// <summary>
		/// Updates the given ticket.
		/// </summary>
		Task<SupportTicket> Update(Context context, SupportTicket ticket);

		/// <summary>
		/// List a filtered set of support tickets.
		/// </summary>
		/// <returns></returns>
		Task<List<SupportTicket>> List(Context context, Filter filter);

	}
}
