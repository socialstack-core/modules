using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.BlogPosts
{
    /// <summary>
    /// Handles blog post endpoints.
    /// </summary>

    [Route("v1/blog/post")]
	[ApiController]
	public partial class BlogPostController : ControllerBase
    {
        private IBlogPostService _blogPosts;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public BlogPostController(
			IBlogPostService blogPosts
        )
        {
            _blogPosts = blogPosts;
        }

		/// <summary>
		/// GET /v1/blog/post/2/
		/// Returns the blogPost data for a single blogPost.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<BlogPost> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _blogPosts.Get(context, id);
			return await Events.BlogPostLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/blog/post/2/
		/// Deletes a blogPost
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _blogPosts.Get(context, id);
			result = await Events.BlogPostDelete.Dispatch(context, result, Response);

			if (result == null || !await _blogPosts.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/blog/post/list
		/// Lists all blog posts available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<BlogPost>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/blog/post/list
		/// Lists filtered blog posts available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<BlogPost>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<BlogPost>(filters);

			filter = await Events.BlogPostList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _blogPosts.List(context, filter);
			return new Set<BlogPost>() { Results = results };
		}

		/// <summary>
		/// POST /v1/blog/post/
		/// Creates a new blog post. Returns the ID.
		/// </summary>
				[HttpPost]
		public async Task<BlogPost> Create([FromBody] BlogPostAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var blogPost = new BlogPost
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, blogPost))
			{
				return null;
			}

			form = await Events.BlogPostCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			blogPost = await _blogPosts.Create(context, form.Result);

			if (blogPost == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return blogPost;
        }
		
		/// <summary>
		/// POST /v1/blog/post/1/
		/// Updates a blog post with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<BlogPost> Update([FromRoute] int id, [FromBody] BlogPostAutoForm form)
		{
			var context = Request.GetContext();

			var blogPost = await _blogPosts.Get(context, id);
			
			if (!ModelState.Setup(form, blogPost)) {
				return null;
			}

			form = await Events.BlogPostUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			blogPost = await _blogPosts.Update(context, form.Result);

			if (blogPost == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return blogPost;
		}
		
    }

}
