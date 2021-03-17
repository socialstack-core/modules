using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs - containers for individual blog posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogService : AutoService<Blog>
    {
		private readonly Query<BlogPost> deletePostsQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public BlogService() : base(Events.Blog)
        {
			deletePostsQuery = Query.Delete<BlogPost>();
			deletePostsQuery.Where().EqualsArg("BlogId", 0);
			
			InstallAdminPages(
				"Blogs", "fa:fa-blog", new string[] { "id", "name" },

				// Each blog page also has a list of blogpost's on it:
				new ChildAdminPageOptions(){
					ChildType = "BlogPost",
					Fields = new string[] { "title" },
					SearchFields = new string[]{ "title" }
				}
				
			);

			// A site has 1 blog unless configured otherwise.
			var config = GetConfig<BlogServiceConfig>();

			Events.BlogPost.BeforeSettable.AddEventListener((Context ctx, JsonField<BlogPost> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<BlogPost>>(field);
				}

				if (field.Name == "BlogId" && !config.MultipleBlogs)
				{
					// Not settable if 1 blog.
					field = null;
				}

				return new ValueTask<JsonField<BlogPost>>(field);
			});

			InstallRoles(new UserRole() {
				Key = "blogger",
				Name = "Blogger",
				CanViewAdmin = true
			});
		}
		
		/// <summary>
		/// Deletes a Blog by its ID, including all its posts
		/// </summary>
		/// <returns></returns>
		public override async ValueTask<bool> Delete(Context context, int id)
        {
			await base.Delete(context, id);
			await _database.Run(context, deletePostsQuery, id);
			
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
			await _database.Run(context, deleteQuery, id);
			
			if(deletePosts){
				await _database.Run(context, deletePostsQuery, id);
			}
			
			// Ok!
			return true;
        }
	}
    
}
