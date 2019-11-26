using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Articles
{
	/// <summary>
	/// Handles articles - containers for individual article posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ArticleService : IArticleService
    {
        private IDatabaseService _database;
		
		private readonly Query<Article> deleteQuery;
		private readonly Query<Article> createQuery;
		private readonly Query<Article> selectQuery;
		private readonly Query<Article> listQuery;
		private readonly Query<Article> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ArticleService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Article>();
			
			createQuery = Query.Insert<Article>();
			updateQuery = Query.Update<Article>();
			selectQuery = Query.Select<Article>();
			listQuery = Query.List<Article>();
		}

		/// <summary>
		/// List a filtered set of articles.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Article>> List(Context context, Filter<Article> filter)
		{
			filter = await Events.ArticleBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ArticleAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a Article by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Gets a single Article by its ID.
		/// </summary>
		public async Task<Article> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new article.
		/// </summary>
		public async Task<Article> Create(Context context, Article article)
		{
			article = await Events.ArticleBeforeCreate.Dispatch(context, article);

			// Note: The Id field is automatically updated by Run here.
			if (article == null || !await _database.Run(createQuery, article))
			{
				return null;
			}

			article = await Events.ArticleAfterCreate.Dispatch(context, article);
			return article;
		}

		/// <summary>
		/// Updates the given article.
		/// </summary>
		public async Task<Article> Update(Context context, Article article)
		{
			article = await Events.ArticleBeforeUpdate.Dispatch(context, article);

			if (article == null || !await _database.Run(updateQuery, article, article.Id))
			{
				return null;
			}

			article = await Events.ArticleAfterUpdate.Dispatch(context, article);
			return article;
		}
	}
    
}
