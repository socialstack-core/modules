using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs - containers for individual blog posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IBlogService
    {
		/// <summary>
		/// Delete a blog by its ID. Optionally also deletes posts.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="deleteContent"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id, bool deleteContent = true);

		/// <summary>
		/// Get a blog by its ID.
		/// </summary>
		Task<Blog> Get(Context context, int id);

		/// <summary>
		/// Create a new blog.
		/// </summary>
		Task<Blog> Create(Context context, Blog blog);

		/// <summary>
		/// Updates the database with the given blog data. It must have an ID set.
		/// </summary>
		Task<Blog> Update(Context context, Blog blog);

		/// <summary>
		/// List a filtered set of blogs.
		/// </summary>
		/// <returns></returns>
		Task<List<Blog>> List(Context context, Filter<Blog> filter);

	}
}
