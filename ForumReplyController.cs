using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.ForumThreads;
using System.Collections.Generic;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.ForumReplies
{
    /// <summary>
    /// Handles forum reply endpoints.
    /// </summary>

    [Route("v1/forum/reply")]
	[ApiController]
	public partial class ForumReplyController : ControllerBase
    {
        private IForumReplyService _forumReplies;
        private IForumThreadService _forumThreads;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ForumReplyController(
            IForumReplyService forumReplies,
			IForumThreadService forumThreads
        )
        {
            _forumReplies = forumReplies;
            _forumThreads = forumThreads;
        }

		/// <summary>
		/// GET /v1/forum/reply/2/
		/// Returns the reply data for a single reply.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ForumReply> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forumReplies.Get(context, id);
			return await Events.ForumReplyLoad.Dispatch(context, result, Response);
		}
		
		/// <summary>
		/// DELETE /v1/forum/reply/2/
		/// Deletes a forum reply
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _forumReplies.Get(context, id);
			result = await Events.ForumReplyDelete.Dispatch(context, result, Response);

			if (result == null || !await _forumReplies.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/forum/reply/list
		/// Lists all forum replies available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<ForumReply>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/forum/reply/list
		/// Lists filtered forum replies available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<ForumReply>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<ForumReply>(filters);

			filter = await Events.ForumReplyList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _forumReplies.List(context, filter);
			return new Set<ForumReply>() { Results = results };
		}

		/// <summary>
		/// POST /v1/forum/reply/
		/// Creates a new forum reply. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<ForumReply> Create([FromBody] ForumReplyAutoForm form)
		{
			var context = Request.GetContext();
			
			// Get the thread so we can grab the forum ID:
			var thread = await _forumThreads.Get(context, form.ThreadId);
			
			if(thread == null){
				return null;
			}
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var forumReply = new ForumReply
			{
				UserId = context.UserId,
				ForumId = thread.ForumId
			};
			
			if (!ModelState.Setup(form, forumReply))
			{
				return null;
			}

			form = await Events.ForumReplyCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			forumReply = await _forumReplies.Create(context, form.Result);

			if (forumReply == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return forumReply;
        }

		/// <summary>
		/// POST /v1/forum/reply/1/
		/// Updates a forum reply with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<ForumReply> Update([FromRoute] int id, [FromBody] ForumReplyAutoForm form)
		{
			var context = Request.GetContext();

			var forumReply = await _forumReplies.Get(context, id);
			
			if (!ModelState.Setup(form, forumReply)) {
				return null;
			}

			form = await Events.ForumReplyUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			forumReply = await _forumReplies.Update(context, form.Result);

			if (forumReply == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return forumReply;
		}
		
    }

}
