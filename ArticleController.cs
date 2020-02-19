using Microsoft.AspNetCore.Mvc;


namespace Api.Articles
{
	/// <summary>
	/// Handles article endpoints.
	/// </summary>
	[Route("v1/article")]
	public partial class ArticleController : AutoController<Article, ArticleAutoForm>
	{
    }
}