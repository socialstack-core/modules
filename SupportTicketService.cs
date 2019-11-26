using Api.Database;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.ForumReplies;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.SupportTickets
{
	/// <summary>
	/// Handles support tickets.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SupportTicketService : ISupportTicketService
	{
        private IDatabaseService _database;
		
		private readonly Query<SupportTicket> deleteQuery;
		private readonly Query<SupportTicket> createQuery;
		private readonly Query<SupportTicket> selectQuery;
		private readonly Query<SupportTicket> updateQuery;
		private readonly Query<SupportTicket> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SupportTicketService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<SupportTicket>();
			
			createQuery = Query.Insert<SupportTicket>();
			updateQuery = Query.Update<SupportTicket>();
			selectQuery = Query.Select<SupportTicket>();
			listQuery = Query.List<SupportTicket>();
		}
		
        /// <summary>
        /// Deletes a ticket by its ID.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteThreads = true)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			if(deleteThreads){
				// Delete the thread too.
			}
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List a filtered set of support tickets.
		/// </summary>
		/// <returns></returns>
		public async Task<List<SupportTicket>> List(Context context, Filter filter)
		{
			return await _database.List(listQuery, filter);
		}

		/// <summary>
		/// Gets a single ticket by its ID.
		/// </summary>
		public async Task<SupportTicket> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new support ticket.
		/// </summary>
		public async Task<SupportTicket> Create(Context context, SupportTicket supportTicket)
		{
			supportTicket = await Events.SupportTicketBeforeCreate.Dispatch(context, supportTicket);

			// Note: The Id field is automatically updated by Run here.
			if (supportTicket == null || !await _database.Run(createQuery, supportTicket))
			{
				return null;
			}

			supportTicket = await Events.SupportTicketAfterCreate.Dispatch(context, supportTicket);
			return supportTicket;
		}

		/// <summary>
		/// Updates the given support ticket.
		/// </summary>
		public async Task<SupportTicket> Update(Context context, SupportTicket supportTicket)
		{
			supportTicket = await Events.SupportTicketBeforeUpdate.Dispatch(context, supportTicket);

			if (supportTicket == null || !await _database.Run(updateQuery, supportTicket, supportTicket.Id))
			{
				return null;
			}

			supportTicket = await Events.SupportTicketAfterUpdate.Dispatch(context, supportTicket);
			return supportTicket;
		}

	}

}
