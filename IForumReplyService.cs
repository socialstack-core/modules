using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Forums
{
	/// <summary>
	/// Handles forum replies.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IForumReplyService
	{
		/// <summary>
		/// Deletes a forum reply by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single reply by its ID.
		/// </summary>
		Task<ForumReply> Get(Context context, int id);

		/// <summary>
		/// Creates a new reply.
		/// </summary>
		Task<ForumReply> Create(Context context, ForumReply reply);

		/// <summary>
		/// Updates the given reply.
		/// </summary>
		Task<ForumReply> Update(Context context, ForumReply reply);

		/// <summary>
		/// List a filtered set of replies.
		/// </summary>
		/// <returns></returns>
		Task<List<ForumReply>> List(Context context, Filter<ForumReply> filter);

	}
}
