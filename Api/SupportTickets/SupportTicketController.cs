using Microsoft.AspNetCore.Mvc;


namespace Api.SupportTickets
{
    /// <summary>
    /// Handles support ticket endpoints.
    /// </summary>
    [Route("v1/supportticket")]
	public partial class SupportTicketController : AutoController<SupportTicket>
    {
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
