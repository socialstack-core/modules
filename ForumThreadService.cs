using Api.Database;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.ForumReplies;
using Api.Permissions;
using Api.Eventing;
using Api.Forums;
using Api.Contexts;

namespace Api.ForumThreads
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumThreadService : IForumThreadService
	{
        private IDatabaseService _database;
		private IForumService _forums;

		private readonly Query<ForumThread> deleteThreadQuery;
		private readonly Query<ForumReply> deleteRepliesQuery;
		private readonly Query<ForumThread> createQuery;
		private readonly Query<ForumThread> selectQuery;
		private readonly Query<ForumThread> updateQuery;
		private readonly Query<ForumThread> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumThreadService(IDatabaseService database, IForumService forums)
        {
            _database = database;
			_forums = forums;

			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteThreadQuery = Query.Delete<ForumThread>();
			deleteRepliesQuery = Query.Delete<ForumReply>();
			deleteRepliesQuery.Where().EqualsArg("ThreadId", 0);
			
			createQuery = Query.Insert<ForumThread>();
			updateQuery = Query.Update<ForumThread>();
			selectQuery = Query.Select<ForumThread>();
			listQuery = Query.List<ForumThread>();
		}

		/// <summary>
		/// List a filtered set of threads.
		/// </summary>
		/// <returns></returns>
		public async Task<List<ForumThread>> List(Context context, Filter<ForumThread> filter)
		{
			filter = await Events.ForumThreadBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ForumThreadAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a forum thread by its ID.
		/// Optionally includes deleting all replies and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteThreads = true)
        {
            // Delete the entry:
			await _database.Run(deleteThreadQuery, id);
			
			if(deleteThreads){
				// Delete their replies:
				await _database.Run(deleteRepliesQuery, id);
			}
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Gets a single thread by its ID.
		/// </summary>
		public async Task<ForumThread> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new forum thread.
		/// </summary>
		public async Task<ForumThread> Create(Context context, ForumThread forumThread)
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

			forumThread = await Events.ForumThreadBeforeCreate.Dispatch(context, forumThread);

			// Note: The Id field is automatically updated by Run here.
			if (forumThread == null || !await _database.Run(createQuery, forumThread))
			{
				return null;
			}

			forumThread = await Events.ForumThreadAfterCreate.Dispatch(context, forumThread);
			return forumThread;
		}

		/// <summary>
		/// Updates the given forum thread.
		/// </summary>
		public async Task<ForumThread> Update(Context context, ForumThread forumThread)
		{
			forumThread = await Events.ForumThreadBeforeUpdate.Dispatch(context, forumThread);

			if (forumThread == null || !await _database.Run(updateQuery, forumThread, forumThread.Id))
			{
				return null;
			}

			forumThread = await Events.ForumThreadAfterUpdate.Dispatch(context, forumThread);
			return forumThread;
		}
	}
    
}
