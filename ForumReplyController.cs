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
	public partial class ForumReplyController : AutoController<ForumReply>
    {
        private IForumThreadService _forumThreads;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ForumReplyController(
			IForumThreadService forumThreads
        )
        {
            _forumThreads = forumThreads;
        }

		/// <summary>
		/// POST /v1/forum/reply/
		/// Creates a new forum reply. Returns the ID.
		/// </summary>
		[HttpPost]
		public override async Task<ForumReply> Create([FromBody] ForumReplyAutoForm form)
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

			form = await Events.ForumReply.Create.Dispatch(context, form, Response) as ForumReplyAutoForm;

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			forumReply = await _service.Create(context, form.Result);

			if (forumReply == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return forumReply;
        }

    }

}
