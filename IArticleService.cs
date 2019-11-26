using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Articles
{
	/// <summary>
	/// Handles articles - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IArticleService
    {
		/// <summary>
		/// Delete a article by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a article by its ID.
		/// </summary>
		Task<Article> Get(Context context, int id);

		/// <summary>
		/// Create a new article.
		/// </summary>
		Task<Article> Create(Context context, Article article);

		/// <summary>
		/// Updates the database with the given article data. It must have an ID set.
		/// </summary>
		Task<Article> Update(Context context, Article article);

		/// <summary>
		/// List a filtered set of articles.
		/// </summary>
		/// <returns></returns>
		Task<List<Article>> List(Context context, Filter<Article> filter);

	}
}
