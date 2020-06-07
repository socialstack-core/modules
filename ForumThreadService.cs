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

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumThreadService : AutoService<ForumThread>, IForumThreadService
	{
		private IForumService _forums;
		private readonly Query<ForumReply> deleteRepliesQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumThreadService(IForumService forums) : base(Events.ForumThread)
        {
			_forums = forums;

			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteRepliesQuery = Query.Delete<ForumReply>();
			deleteRepliesQuery.Where().EqualsArg("ThreadId", 0);
			
			InstallAdminPages(new string[] { "id", "title", "createdDateUtc" });
		}

		/// <summary>
		/// Deletes a forum thread by its ID.
		/// Optionally includes deleting all replies and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public override async Task<bool> Delete(Context context, int id)
        {
            // Delete the entry:
			await _database.Run(context, deleteQuery, id);
			
			// Delete their replies:
			await _database.Run(context, deleteRepliesQuery, id);
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Deletes a forum thread by its ID.
		/// Optionally includes deleting all replies and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteThreads)
        {
            // Delete the entry:
			await base.Delete(context, id);
			
			if(deleteThreads){
				// Delete their replies:
				await _database.Run(context, deleteRepliesQuery, id);
			}
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Creates a new forum thread.
		/// </summary>
		public override async Task<ForumThread> Create(Context context, ForumThread forumThread)
		{
			// Get the forum to obtain the default page ID:
			var forum = await _forums.Get(context, forumThread.ForumId);

			if (forum == null)
			{
				// Forum doesn't exist!
				return null;
			}

			if (forumThread.PageId == 0)
			{
				// Default page ID applied now:
				forumThread.PageId = forum.ThreadPageId;
			}
			
			return await base.Create(context, forumThread);
		}
	}

}
