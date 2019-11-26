using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.ForumThreads
{
    /// <summary>
    /// Handles forum thread endpoints.
    /// </summary>

    [Route("v1/forum/thread")]
	[ApiController]
	public partial class ForumThreadController : ControllerBase
    {
        private IForumThreadService _forumThreads;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ForumThreadController(
			IForumThreadService forumThreads
        )
        {
            _forumThreads = forumThreads;
        }

		/// <summary>
		/// GET /v1/forum/thread/2/
		/// Returns the thread data for a single thread.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ForumThread> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forumThreads.Get(context, id);
			return await Events.ForumThreadLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/forum/thread/2/
		/// Deletes a thread
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forumThreads.Get(context, id);
			result = await Events.ForumThreadDelete.Dispatch(context, result, Response);

			if (result == null || !await _forumThreads.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/forum/thread/list
		/// Lists all forum threads available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<ForumThread>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/forum/thread/list
		/// Lists filtered forum threads available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<ForumThread>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<ForumThread>(filters);

			filter = await Events.ForumThreadList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _forumThreads.List(context, filter);
			return new Set<ForumThread>() { Results = results };
		}

		/// <summary>
		/// POST /v1/forum/thread/
		/// Creates a new forum thread. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<ForumThread> Create([FromBody] ForumThreadAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var forumThread = new ForumThread
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, forumThread))
			{
				return null;
			}

			form = await Events.ForumThreadCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			forumThread = await _forumThreads.Create(context, form.Result);

			if (forumThread == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return forumThread;
        }
		
		/// <summary>
		/// POST /v1/forum/thread/1/
		/// Updates a forum thread with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<ForumThread> Update([FromRoute] int id, [FromBody] ForumThreadAutoForm form)
		{
			var context = Request.GetContext();

			var forumThread = await _forumThreads.Get(context, id);
			
			if (!ModelState.Setup(form, forumThread)) {
				return null;
			}

			form = await Events.ForumThreadUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			forumThread = await _forumThreads.Update(context, form.Result);

			if (forumThread == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return forumThread;
		}
		
    }

}
