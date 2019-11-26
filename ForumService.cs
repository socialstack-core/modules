using Api.Database;
using System.Threading.Tasks;
using Api.ForumThreads;
using Api.ForumReplies;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forums - containers for forum threads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumService : IForumService
    {
        private IDatabaseService _database;
		
		private readonly Query<Forum> deleteForumQuery;
		private readonly Query<ForumThread> deleteThreadsQuery;
		private readonly Query<ForumReply> deleteRepliesQuery;
		private readonly Query<Forum> createQuery;
		private readonly Query<Forum> selectQuery;
		private readonly Query<Forum> listQuery;
		private readonly Query<Forum> updateQuery;
		private readonly Query<Forum> updateThreadCountQuery;
		private readonly Query<Forum> updateReplyCountQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteForumQuery = Query.Delete<Forum>();

			deleteThreadsQuery = Query.Delete<ForumThread>();
			deleteThreadsQuery.Where().EqualsArg("ForumId", 0);

			deleteRepliesQuery = Query.Delete<ForumReply>();
			deleteRepliesQuery.Where().EqualsArg("ForumId", 0);
			
			createQuery = Query.Insert<Forum>();
			updateQuery = Query.Update<Forum>();
			selectQuery = Query.Select<Forum>();
			listQuery = Query.List<Forum>();
			
			updateThreadCountQuery = Query.Update<Forum>().RemoveAllBut("Id", "ThreadCount");
			updateReplyCountQuery = Query.Update<Forum>().RemoveAllBut("Id", "ReplyCount");
			
			// Add some events to bump our cached counters whenever a reply/ thread is added:
			Events.ForumThreadAfterCreate.AddEventListener(async (Context context, ForumThread thread) =>
			{
				await _database.Run(updateThreadCountQuery, thread.ForumId, 1);

				return thread;
			});
			
			Events.ForumReplyAfterCreate.AddEventListener(async (Context context, ForumReply reply) =>
			{
				await _database.Run(updateReplyCountQuery, reply.ForumId, 1);

				return reply;
			});
		}
		
        /// <summary>
        /// Deletes a forum by its ID.
		/// Optionally includes deleting all replies, threads and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteThreads = true)
        {
            // Delete the forum entry:
			await _database.Run(deleteForumQuery, id);
			
			if(deleteThreads){
				// Delete threads:
				await _database.Run(deleteThreadsQuery, id);
				
				// Delete their replies:
				await _database.Run(deleteRepliesQuery, id);
			}
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List a filtered set of forums.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Forum>> List(Context context, Filter<Forum> filter)
		{
			filter = await Events.ForumBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ForumAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Gets a single forum by its ID.
		/// </summary>
		public async Task<Forum> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new forum.
		/// </summary>
		public async Task<Forum> Create(Context context, Forum forum)
		{
			forum = await Events.ForumBeforeCreate.Dispatch(context, forum);

			// Note: The Id field is automatically updated by Run here.
			if (forum == null || !await _database.Run(createQuery, forum))
			{
				return null;
			}

			forum = await Events.ForumAfterCreate.Dispatch(context, forum);
			return forum;
		}

		/// <summary>
		/// Updates the given forum.
		/// </summary>
		public async Task<Forum> Update(Context context, Forum forum)
		{
			forum = await Events.ForumBeforeUpdate.Dispatch(context, forum);

			if (forum == null || !await _database.Run(updateQuery, forum, forum.Id))
			{
				return null;
			}

			forum = await Events.ForumAfterUpdate.Dispatch(context, forum);
			return forum;
		}
	}
    
}
