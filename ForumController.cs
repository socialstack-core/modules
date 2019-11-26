using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum endpoints.
    /// </summary>

    [Route("v1/forum")]
	[ApiController]
	public partial class ForumController : ControllerBase
    {
        private IForumService _forums;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ForumController(
            IForumService forums
        )
        {
            _forums = forums;
        }

		/// <summary>
		/// GET /v1/forum/2/
		/// Returns the forum data for a single forum.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Forum> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forums.Get(context, id);
			return await Events.ForumLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/forum/2/
		/// Deletes a forum
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forums.Get(context, id);
			result = await Events.ForumDelete.Dispatch(context, result, Response);

			if (result == null || !await _forums.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/forum/list
		/// Lists all forums available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Forum>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/forum/list
		/// Lists filtered forums available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Forum>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Forum>(filters);

			filter = await Events.ForumList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _forums.List(context, filter);
			return new Set<Forum>() { Results = results };
		}

		/// <summary>
		/// POST /v1/forum/
		/// Creates a new forum. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Forum> Create([FromBody] ForumAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var forum = new Forum
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, forum))
			{
				return null;
			}

			form = await Events.ForumCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			forum = await _forums.Create(context, form.Result);

			if (forum == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return forum;
        }

		/// <summary>
		/// POST /v1/forum/1/
		/// Updates a forum with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Forum> Update([FromRoute] int id, [FromBody] ForumAutoForm form)
		{
			var context = Request.GetContext();

			var forum = await _forums.Get(context, id);
			
			if (!ModelState.Setup(form, forum)) {
				return null;
			}

			form = await Events.ForumUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			forum = await _forums.Update(context, form.Result);

			if (forum == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return forum;
		}
		
    }

}
