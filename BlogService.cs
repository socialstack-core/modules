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
	public partial class BlogService : IBlogService
    {
        private IDatabaseService _database;
		
		private readonly Query<Blog> deleteQuery;
		private readonly Query<BlogPost> deletePostsQuery;
		private readonly Query<Blog> createQuery;
		private readonly Query<Blog> selectQuery;
		private readonly Query<Blog> listQuery;
		private readonly Query<Blog> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public BlogService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Blog>();
			deletePostsQuery = Query.Delete<BlogPost>();
			deletePostsQuery.Where().EqualsArg("BlogId", 0);

			createQuery = Query.Insert<Blog>();
			updateQuery = Query.Update<Blog>();
			selectQuery = Query.Select<Blog>();
			listQuery = Query.List<Blog>();
		}

		/// <summary>
		/// List a filtered set of blogs.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Blog>> List(Context context, Filter<Blog> filter)
		{
			filter = await Events.BlogBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.BlogAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a Blog by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteUploads = true)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			if(deleteUploads){
				await _database.Run(deletePostsQuery, id);
			}
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Gets a single Blog by its ID.
		/// </summary>
		public async Task<Blog> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new blog.
		/// </summary>
		public async Task<Blog> Create(Context context, Blog blog)
		{
			blog = await Events.BlogBeforeCreate.Dispatch(context, blog);

			// Note: The Id field is automatically updated by Run here.
			if (blog == null || !await _database.Run(createQuery, blog))
			{
				return null;
			}

			blog = await Events.BlogAfterCreate.Dispatch(context, blog);
			return blog;
		}

		/// <summary>
		/// Updates the given blog.
		/// </summary>
		public async Task<Blog> Update(Context context, Blog blog)
		{
			blog = await Events.BlogBeforeUpdate.Dispatch(context, blog);

			if (blog == null || !await _database.Run(updateQuery, blog, blog.Id))
			{
				return null;
			}

			blog = await Events.BlogAfterUpdate.Dispatch(context, blog);
			return blog;
		}
	}
    
}
