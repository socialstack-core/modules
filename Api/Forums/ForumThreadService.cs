using Api.Database;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumThreadService : AutoService<ForumThread>//, IForumThreadService
	{
		private ForumService _forums;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumThreadService(ForumService forums) : base(Events.ForumThread)
        {
			_forums = forums;

			InstallAdminPages(null, null, new string[] { "id", "title", "createdUtc" });
			
			// Connect a create event:
			Events.ForumThread.BeforeCreate.AddEventListener(async (Context context, ForumThread thread) => {
				
				// Get the forum:
				var forum = await _forums.Get(context, thread.ForumId);
				
				if (forum == null) {
					throw new PublicException("Forum not found", "forum/notfound");
				}

				return thread;
			}, 9);

			Events.Forum.AfterDelete.AddEventListener(async (Context context, Forum forum) =>
			{
				// Delete all threads in this forum (will then implicitly delete all replies):
				var allThreads = await Where("ForumId=?", DataOptions.IgnorePermissions).Bind(forum.Id).ListAll(context);

				foreach (var thread in allThreads)
				{
					await Delete(context, thread);
				}

				return forum;
			});
		}
	}

}
