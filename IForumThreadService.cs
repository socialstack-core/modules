using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IForumThreadService
	{
		/// <summary>
		/// Deletes a forum thread by its ID.
		/// Optionally includes deleting all replies and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int threadId, bool deleteReplies = true);

		/// <summary>
		/// Gets a single thread by its ID.
		/// </summary>
		Task<ForumThread> Get(Context context, int threadId);

		/// <summary>
		/// Creates a new thread.
		/// </summary>
		Task<ForumThread> Create(Context context, ForumThread thread);

		/// <summary>
		/// Updates the given thread.
		/// </summary>
		Task<ForumThread> Update(Context context, ForumThread thread);

		/// <summary>
		/// List a filtered set of threads.
		/// </summary>
		/// <returns></returns>
		Task<List<ForumThread>> List(Context context, Filter<ForumThread> filter);

	}
}
