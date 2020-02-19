using Api.Blogs;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.BlogPosts
{
	/// <summary>
	/// Handles blogs posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogPostService : AutoService<BlogPost>, IBlogPostService
    {
        private IBlogService _blogs;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="database"></param>
		/// <param name="blogs"></param>
		public BlogPostService(IBlogService blogs) : base(Events.BlogPost)
        {
			_blogs = blogs;
		}

		/// <summary>
		/// Creates a new blog post.
		/// </summary>
		public override async Task<BlogPost> Create(Context context, BlogPost post)
		{
			// Get the blog to obtain the default page ID:
			var blog = await _blogs.Get(context, post.BlogId);

			if (blog == null)
			{
				// Blog doesn't exist!
				return null;
			}
			
			if (post.PageId == 0)
			{
				// Default page ID applied now:
				post.PageId = blog.PostPageId;
			}
			
			return await base.Create(context, post);
		}
		
	}

}
