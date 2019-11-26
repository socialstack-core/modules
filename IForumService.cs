using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forums - containers for forum threads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IForumService
    {
        /// <summary>
        /// Deletes a forum by its ID.
		/// Optionally includes deleting all replies, threads and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
		Task<bool> Delete(Context context, int forumId, bool deleteThreads = true);

		/// <summary>
		/// Gets a single forum by its ID.
		/// </summary>
		Task<Forum> Get(Context context, int forumId);

		/// <summary>
		/// Creates a new forum.
		/// </summary>
		Task<Forum> Create(Context context, Forum forum);

		/// <summary>
		/// Updates the given forum.
		/// </summary>
		Task<Forum> Update(Context context, Forum forum);

		/// <summary>
		/// List a filtered set of forums.
		/// </summary>
		/// <returns></returns>
		Task<List<Forum>> List(Context context, Filter<Forum> filter);

	}
}
