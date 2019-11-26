using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Pages
{
    /// <summary>
    /// Handles page endpoints.
    /// </summary>

    [Route("v1/page")]
	[ApiController]
	public partial class PageController : ControllerBase
    {
        private IPageService _pages;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public PageController(
			IPageService pages

		)
        {
			_pages = pages;
        }

		/// <summary>
		/// GET /v1/page/2/
		/// Returns the  data for a single page.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Page> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _pages.Get(context, id);
			return await Events.PageLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/page/2/
		/// Deletes a page
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _pages.Get(context, id);
			result = await Events.PageDelete.Dispatch(context, result, Response);

			if (result == null || !await _pages.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/page/list
		/// Lists all pages available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Page>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/page/list
		/// Lists filtered pages available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Page>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Page>(filters);

			filter = await Events.PageList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _pages.List(context, filter);
			return new Set<Page>() { Results = results };
		}

		/// <summary>
		/// POST /v1/page/
		/// Creates a new page. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Page> Create([FromBody] PageAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var page = new Page
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, page))
			{
				return null;
			}

			form = await Events.PageCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			page = await _pages.Create(context, form.Result);

			if (page == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return page;
        }

		/// <summary>
		/// POST /v1/page/1/
		/// Updates a page with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Page> Update([FromRoute] int id, [FromBody] PageAutoForm form)
		{
			var context = Request.GetContext();

			var page = await _pages.Get(context, id);
			
			if (!ModelState.Setup(form, page)) {
				return null;
			}

			form = await Events.PageUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			page = await _pages.Update(context, form.Result);

			if (page == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return page;
		}

    }

}
