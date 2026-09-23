using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
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
	public partial class ForumReplyService : AutoService<ForumReply>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumReplyService(ForumThreadService forumThreads, ForumService forums) : base(Events.ForumReply)
        {
			InstallAdminPages("Forums", "fa:fa-comments", new string[] { "id", "createdUtc" });
			
			Events.ForumReply.BeforeCreate.AddEventListener(async (Context context, ForumReply reply) => {
				
				// Get the thread so we can ensure the forum ID is correct:
				var thread = await forumThreads.Get(context, reply.ThreadId);
				
				if (thread == null) {
					throw new PublicException("Thread not found", "thread/notfound");
				}
				
				var forum = await forums.Get(context, thread.ForumId);
				
				if(forum == null){
					throw new PublicException("Forum not found", "forum/notfound");
				}

				reply.ForumId = thread.ForumId;

				return reply;
			}, 9);

			Events.ForumThread.AfterDelete.AddEventListener(async (Context context, ForumThread forumThread) =>
			{
				// Delete all replies in this thread:
				var allReplies = await Where("ThreadId=?", DataOptions.IgnorePermissions).Bind(forumThread.Id).ListAll(context);

				foreach (var reply in allReplies)
				{
					await Delete(context, reply);
				}

				return forumThread;
			});
		}
	}
}