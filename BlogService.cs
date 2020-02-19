using Api.Database;
using System.Threading.Tasks;
using Api.BlogPosts;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs - containers for individual blog posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogService : AutoService<Blog>, IBlogService
    {
		private readonly Query<BlogPost> deletePostsQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public BlogService() : base(Events.Blog)
        {
			deletePostsQuery = Query.Delete<BlogPost>();
			deletePostsQuery.Where().EqualsArg("BlogId", 0);
		}
		
		/// <summary>
		/// Deletes a Blog by its ID, including all its posts
		/// </summary>
		/// <returns></returns>
		public override async Task<bool> Delete(Context context, int id)
        {
			await base.Delete(context, id);
			await _database.Run(deletePostsQuery, id);
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Deletes a Blog by its ID.
		/// Optionally deletes the posts.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deletePosts = true)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			if(deletePosts){
				await _database.Run(deletePostsQuery, id);
			}
			
			// Ok!
			return true;
        }
	}
    
}
