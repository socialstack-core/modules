using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Forums
{
	/// <summary>
	/// Handles forum replies.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumReplyService : AutoService<ForumReply>//, IForumReplyService
    {
		private readonly Query<Forum> updateReplyCountQuery;
		private readonly Query<ForumThread> updateThreadReplyQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumReplyService(ForumThreadService forumThreads, ForumService forums) : base(Events.ForumReply)
        {
			InstallAdminPages(new string[] { "id", "createdDateUtc" });
			
			updateReplyCountQuery = Query.Update<Forum>().RemoveAllBut("Id", "ReplyCount");
			updateThreadReplyQuery = Query.Update<ForumThread>().RemoveAllBut("Id", "ReplyCount");
			
			// Connect a create event:
			Events.ForumReply.BeforeCreate.AddEventListener(async (Context context, ForumReply reply) => {
				
				// Get the thread so we can ensure the forum ID is correct:
				var thread = await forumThreads.Get(context, reply.ThreadId);
				
				if (thread == null) {
					return null;
				}
				
				var forum = await forums.Get(context, thread.ForumId);
				
				if(forum == null){
					return null;
				}
				
				await _database.Run(context, updateReplyCountQuery, forum.ReplyCount + 1, forum.Id);
				await _database.Run(context, updateThreadReplyQuery, thread.ReplyCount + 1, thread.Id);
				
				reply.ForumId = thread.ForumId;
				return reply;
			});

        }
	}
}