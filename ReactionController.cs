using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Reactions
{
    /// <summary>
    /// Handles reaction endpoints (liking, disliking, upvoting, downvoting, love hearting etc).
    /// </summary>

    [Route("v1/reaction")]
	[ApiController]
	public partial class ReactionController : ControllerBase
    {
        private IReactionService _reactions;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ReactionController(
			IReactionService reactions
		)
        {
			_reactions = reactions;
        }

		/// <summary>
		/// GET /v1/reaction/2/
		/// Returns the data for a single reaction.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Reaction> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _reactions.Get(context, id);
			return await Events.ReactionLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/reaction/2/
		/// Deletes a reaction
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _reactions.Get(context, id);
			result = await Events.ReactionDelete.Dispatch(context, result, Response);

			if (result == null || !await _reactions.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/reaction/list
		/// Lists all reactions available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Reaction>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/reaction/list
		/// Lists filtered reactions available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Reaction>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Reaction>(filters);

			filter = await Events.ReactionList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _reactions.List(context, filter);
			return new Set<Reaction>() { Results = results };
		}

		/// <summary>
		/// POST /v1/reaction/
		/// Creates a new reaction. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Reaction> Create([FromBody] ReactionAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var reaction = new Reaction
			{
				UserId = context.UserId
			};
			
			reaction.ContentTypeId = ContentTypes.GetId(form.ContentType);
			
			if (!ModelState.Setup(form, reaction))
			{
				return null;
			}

			form = await Events.ReactionCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			reaction = await _reactions.Create(context, form.Result);

			if (reaction == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return reaction;
        }

		/// <summary>
		/// POST /v1/reaction/1/
		/// Updates a reaction with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Reaction> Update([FromRoute] int id, [FromBody] ReactionAutoForm form)
		{
			var context = Request.GetContext();

			var reaction = await _reactions.Get(context, id);
			
			if (!ModelState.Setup(form, reaction)) {
				return null;
			}

			form = await Events.ReactionUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			reaction = await _reactions.Update(context, form.Result);

			if (reaction == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return reaction;
		}
		
    }

}
