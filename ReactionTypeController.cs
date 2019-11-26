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
    /// Handles reaction types (creating a new type of like/ upvote etc).
    /// </summary>

    [Route("v1/reaction/type")]
	[ApiController]
	public partial class ReactionTypeController : ControllerBase
    {
        private IReactionTypeService _reactionTypes;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ReactionTypeController(
			IReactionTypeService reactions
		)
        {
			_reactionTypes = reactions;
        }

		/// <summary>
		/// GET /v1/reaction/type/2/
		/// Returns the data for a single reaction type.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ReactionType> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _reactionTypes.Get(context, id);
			return await Events.ReactionTypeLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/reaction/type/2/
		/// Deletes a reaction type
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _reactionTypes.Get(context, id);
			result = await Events.ReactionTypeDelete.Dispatch(context, result, Response);

			if (result == null || !await _reactionTypes.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/reaction/type/list
		/// Lists all reactions available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<ReactionType>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/reaction/type/list
		/// Lists filtered reaction types available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<ReactionType>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<ReactionType>(filters);

			filter = await Events.ReactionTypeList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _reactionTypes.List(context, filter);
			return new Set<ReactionType>() { Results = results };
		}

		/// <summary>
		/// POST /v1/reaction/type/
		/// Creates a new reaction type. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<ReactionType> Create([FromBody] ReactionTypeAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var reactionType = new ReactionType
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, reactionType))
			{
				return null;
			}

			form = await Events.ReactionTypeCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			reactionType = await _reactionTypes.Create(context, form.Result);

			if (reactionType == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return reactionType;
        }

		/// <summary>
		/// POST /v1/reaction/type/1/
		/// Updates a reaction type with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<ReactionType> Update([FromRoute] int id, [FromBody] ReactionTypeAutoForm form)
		{
			var context = Request.GetContext();

			var reactionType = await _reactionTypes.Get(context, id);
			
			if (!ModelState.Setup(form, reactionType)) {
				return null;
			}

			form = await Events.ReactionTypeUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			reactionType = await _reactionTypes.Update(context, form.Result);

			if (reactionType == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return reactionType;
		}
		
    }

}
