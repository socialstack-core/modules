using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace Api.Forums
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

			// Connect a create event:
			Events.ForumReply.BeforeCreate.AddEventListener(async (Context context, ForumReply reply) => {
				
				// Get the thread so we can ensure the forum ID is correct:
				var thread = await _forumThreads.Get(context, reply.ThreadId);
				
				if (thread == null) {
					return null;
				}
				
				reply.ForumId = thread.ForumId;
				return reply;
			});

		}
	}

}
