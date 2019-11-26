using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blog endpoints.
    /// </summary>

    [Route("v1/blog")]
	[ApiController]
	public partial class BlogController : ControllerBase
    {
        private IBlogService _blogs;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public BlogController(
			IBlogService blogs

		)
        {
			_blogs = blogs;
        }

		/// <summary>
		/// GET /v1/blog/2/
		/// Returns the blog data for a single blog.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Blog> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _blogs.Get(context, id);
			return await Events.BlogLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/blog/2/
		/// Deletes a blog
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _blogs.Get(context, id);
			result = await Events.BlogDelete.Dispatch(context, result, Response);

			if (result == null || !await _blogs.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/blog/list
		/// Lists all blogs available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Blog>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/blog/list
		/// Lists filtered blogs available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Blog>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Blog>(filters);

			filter = await Events.BlogList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _blogs.List(context, filter);
			return new Set<Blog>() { Results = results };
		}

		/// <summary>
		/// POST /v1/blog/
		/// Creates a new blog. Returns the ID.
		/// </summary>
				[HttpPost]
		public async Task<Blog> Create([FromBody] BlogAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var blog = new Blog
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, blog))
			{
				return null;
			}

			form = await Events.BlogCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			blog = await _blogs.Create(context, form.Result);

			if (blog == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return blog;
        }

		/// <summary>
		/// POST /v1/blog/1/
		/// Updates a blog with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Blog> Update([FromRoute] int id, [FromBody] BlogAutoForm form)
		{
			var context = Request.GetContext();

			var blog = await _blogs.Get(context, id);
			
			if (!ModelState.Setup(form, blog)) {
				return null;
			}

			form = await Events.BlogUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			blog = await _blogs.Update(context, form.Result);

			if (blog == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return blog;
		}
		
    }

}
