using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.ForumThreads;
using System;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.SupportTickets
{
    /// <summary>
    /// Handles support ticket endpoints.
    /// </summary>

    [Route("v1/support/ticket")]
	[ApiController]
	public partial class SupportTicketController : ControllerBase
    {
        private ISupportTicketService _supportTickets;
        private IForumThreadService _forumThreads;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public SupportTicketController(
			ISupportTicketService supportTickets,
			IForumThreadService forumThreads
		)
        {
			_supportTickets = supportTickets;
			_forumThreads = forumThreads;
		}

		/// <summary>
		/// GET /v1/support/ticket/2/
		/// Returns the data for a single support ticket.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<SupportTicket> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _supportTickets.Get(context, id);
			return await Events.SupportTicketLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/support/ticket/2/
		/// Deletes a ticket
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _supportTickets.Get(context, id);
			result = await Events.SupportTicketDelete.Dispatch(context, result, Response);

			if (result == null || !await _supportTickets.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/support/ticket/list
		/// Lists all support tickets available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<SupportTicket>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/support/ticket/list
		/// Lists filtered support tickets available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<SupportTicket>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<SupportTicket>(filters);

			filter = await Events.SupportTicketList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _supportTickets.List(context, filter);
			return new Set<SupportTicket>() { Results = results };
		}

		/*
		/// <summary>
		/// POST /v1/support/ticket/
		/// Creates a new support ticket. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<object> Create([FromBody] SupportTicketCreate body)
		{
			var context = Request.GetContext();
			// Create a thread and a support ticket.
			var thread = new ForumThread
			{
				UserId = Request.GetUserId(),
				CreatedUtc = DateTime.UtcNow,
				BodyJson = body.BodyJson,
				Title = body.Title,
				ForumId = body.ForumId,
				IsSupportTicket = true
			};

			thread = await Events.SupportTicketThreadCreate.Dispatch(context, thread, Response);

			if (thread == null)
			{
				// A handler rejected this request.
				return null;
			}

			thread = await _forumThreads.Create(context, thread);

			if (thread == null)
			{
				Response.StatusCode = 500;
				return null;
			}

			// The create call above sets the thread ID for us. 
			// We use that same ID for our support ticket entry too:
			var ticket = new SupportTicket
			{
				Id = thread.Id
			};

			ticket = await Events.SupportTicketCreate.Dispatch(context, ticket, Response);

			if (ticket == null)
			{
				// A handler rejected this request.
				return null;
			}

			ticket = await _supportTickets.Create(context, ticket);

			if (ticket == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            var id = ticket.Id;
			
			return new {
				id,
				threadId = thread.Id
			};
        }
		*/

    }

}
