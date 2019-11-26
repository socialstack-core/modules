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
	public partial class BlogPostService : IBlogPostService
    {
        private IDatabaseService _database;
        private IBlogService _blogs;
		
		private readonly Query<BlogPost> deleteQuery;
		private readonly Query<BlogPost> createQuery;
		private readonly Query<BlogPost> selectQuery;
		private readonly Query<BlogPost> listQuery;
		private readonly Query<BlogPost> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="database"></param>
		/// <param name="blogs"></param>
		public BlogPostService(IDatabaseService database, IBlogService blogs)
        {
            _database = database;
			_blogs = blogs;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<BlogPost>();
			createQuery = Query.Insert<BlogPost>();
			updateQuery = Query.Update<BlogPost>();
			selectQuery = Query.Select<BlogPost>();
			listQuery = Query.List<BlogPost>();
		}

		/// <summary>
		/// List a filtered set of blog posts.
		/// </summary>
		/// <returns></returns>
		public async Task<List<BlogPost>> List(Context context, Filter<BlogPost> filter)
		{
			filter = await Events.BlogPostBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.BlogPostAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a blog post by its ID.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteContent = true)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			if(deleteContent)
			{
			}
			
			// Ok!
			return true;
        }

		/// <summary>
		/// Gets a single blog post by its ID.
		/// </summary>
		public async Task<BlogPost> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new blog post.
		/// </summary>
		public async Task<BlogPost> Create(Context context, BlogPost post)
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

			post = await Events.BlogPostBeforeCreate.Dispatch(context, post);

			// Note: The Id field is automatically updated by Run here.
			if (post == null || !await _database.Run(createQuery, post))
			{
				return null;
			}

			post = await Events.BlogPostAfterCreate.Dispatch(context, post);
			return post;
		}

		/// <summary>
		/// Updates the given blog post.
		/// </summary>
		public async Task<BlogPost> Update(Context context, BlogPost post)
		{
			post = await Events.BlogPostBeforeUpdate.Dispatch(context, post);

			if (post == null || !await _database.Run(updateQuery, post, post.Id))
			{
				return null;
			}

			post = await Events.BlogPostAfterUpdate.Dispatch(context, post);
			return post;
		}

	}

}
