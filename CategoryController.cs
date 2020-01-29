using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Categories
{
    /// <summary>
    /// Handles category endpoints.
    /// </summary>

    [Route("v1/category")]
	[ApiController]
	public partial class CategoryController : ControllerBase
    {
        private ICategoryService _categories;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public CategoryController(
			ICategoryService categories

		)
        {
			_categories = categories;
        }

		/// <summary>
		/// GET /v1/category/2/
		/// Returns the category data for a single category.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Category> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _categories.Get(context, id);
			return await Events.CategoryLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/category/2/
		/// Deletes an category
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Category> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _categories.Get(context, id);
			result = await Events.CategoryDelete.Dispatch(context, result, Response);

			if (result == null || !await _categories.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return result;
		}

		/// <summary>
		/// GET /v1/category/list
		/// Lists all categories available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Category>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/category/list
		/// Lists filtered categories available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Category>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Category>(filters);

			filter = await Events.CategoryList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _categories.List(context, filter);
			return new Set<Category>() { Results = results };
		}

		/// <summary>
		/// POST /v1/category/
		/// Creates a new category. Returns the ID.
		/// </summary>
		[HttpPost]
				[HttpPost]
		public async Task<Category> Create([FromBody] CategoryAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var category = new Category
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, category))
			{
				return null;
			}

			form = await Events.CategoryCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			category = await _categories.Create(context, form.Result);

			if (category == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return category;
        }

		/// <summary>
		/// POST /v1/category/1/
		/// Updates a category with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Category> Update([FromRoute] int id, [FromBody] CategoryAutoForm form)
		{
			var context = Request.GetContext();

			var category = await _categories.Get(context, id);
			
			if (!ModelState.Setup(form, category)) {
				return null;
			}

			form = await Events.CategoryUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			category = await _categories.Update(context, form.Result);

			if (category == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return category;
		}
		
    }

}
