using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.BlogPosts
{
	/// <summary>
	/// Handles blogs posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IBlogPostService
	{
		/// <summary>
		/// Deletes a blog post by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int postId);

		/// <summary>
		/// Gets a single blog post by its ID.
		/// </summary>
		Task<BlogPost> Get(Context context, int postId);

		/// <summary>
		/// Creates a new blog post.
		/// </summary>
		Task<BlogPost> Create(Context context, BlogPost post);
		
		/// <summary>
		/// Updates the given blog post.
		/// </summary>
		Task<BlogPost> Update(Context context, BlogPost post);

		/// <summary>
		/// List a filtered set of blog posts.
		/// </summary>
		/// <returns></returns>
		Task<List<BlogPost>> List(Context context, Filter<BlogPost> filter);
	}
}
