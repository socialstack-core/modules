using Api.Database;
using System.Threading.Tasks;
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
	public partial class ForumService : AutoService<Forum>, IForumService
    {
		private readonly Query<ForumThread> deleteThreadsQuery;
		private readonly Query<ForumReply> deleteRepliesQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumService() : base(Events.Forum)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteThreadsQuery = Query.Delete<ForumThread>();
			deleteThreadsQuery.Where().EqualsArg("ForumId", 0);

			deleteRepliesQuery = Query.Delete<ForumReply>();
			deleteRepliesQuery.Where().EqualsArg("ForumId", 0);
			
			InstallAdminPages("Forums", "fa:fa-th-list", new string[] { "id", "name" });
		}
		
        /// <summary>
        /// Deletes a forum by its ID.
		/// Includes deleting all replies, threads and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Delete(Context context, int id)
        {
            // Delete the forum entry:
			await base.Delete(context, id);
			
			// Delete threads:
			await _database.Run(context, deleteThreadsQuery, id);
			
			// Delete their replies:
			await _database.Run(context, deleteRepliesQuery, id);
			
			// Ok!
			return true;
        }
		
        /// <summary>
        /// Deletes a forum by its ID.
		/// Optionally includes deleting all replies, threads and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteThreads)
        {
            // Delete the forum entry:
			await base.Delete(context, id);
			
			if(deleteThreads){
				// Delete threads:
				await _database.Run(context, deleteThreadsQuery, id);
				
				// Delete their replies:
				await _database.Run(context, deleteRepliesQuery, id);
			}
			
			// Ok!
			return true;
        }
	}
    
}
