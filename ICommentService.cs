using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Comments
{
	/// <summary>
	/// Handles comments.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ICommentService
	{
		/// <summary>
		/// Deletes an comment by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single comment by its ID.
		/// </summary>
		Task<Comment> Get(Context context, int id);

		/// <summary>
		/// Creates a new comment.
		/// </summary>
		Task<Comment> Create(Context context, Comment comment);

		/// <summary>
		/// Updates the given comment.
		/// </summary>
		Task<Comment> Update(Context context, Comment comment);

		/// <summary>
		/// List a filtered set of comments.
		/// </summary>
		/// <returns></returns>
		Task<List<Comment>> List(Context context, Filter<Comment> filter);
	}
}
