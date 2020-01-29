using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Articles
{
    /// <summary>
    /// Handles article endpoints.
    /// </summary>

    [Route("v1/article")]
	[ApiController]
	public partial class ArticleController : ControllerBase
    {
        private IArticleService _articles;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ArticleController(
			IArticleService articles

		)
        {
			_articles = articles;
        }

		/// <summary>
		/// GET /v1/article/2/
		/// Returns the article data for a single article.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Article> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _articles.Get(context, id);
			return await Events.ArticleLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/article/2/
		/// Deletes an article
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Article> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _articles.Get(context, id);
			result = await Events.ArticleDelete.Dispatch(context, result, Response);

			if (result == null || !await _articles.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
			return result;
		}

		/// <summary>
		/// GET /v1/article/list
		/// Lists all articles available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Article>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/article/list
		/// Lists filtered articles available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Article>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Article>(filters);

			filter = await Events.ArticleList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _articles.List(context, filter);
			return new Set<Article>() { Results = results };
		}

		/// <summary>
		/// POST /v1/article/
		/// Creates a new article. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Article> Create([FromBody] ArticleAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var article = new Article
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, article))
			{
				return null;
			}

			form = await Events.ArticleCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			article = await _articles.Create(context, form.Result);

			if (article == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return article;
        }

		/// <summary>
		/// POST /v1/article/1/
		/// Updates an article with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Article> Update([FromRoute] int id, [FromBody] ArticleAutoForm form)
		{
			var context = Request.GetContext();

			var article = await _articles.Get(context, id);
			
			if (!ModelState.Setup(form, article)) {
				return null;
			}

			form = await Events.ArticleUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			article = await _articles.Update(context, form.Result);

			if (article == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return article;
		}
		
    }

}
