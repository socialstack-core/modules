using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Tags
{
    /// <summary>
    /// Handles tag endpoints.
    /// </summary>

    [Route("v1/tag")]
	[ApiController]
	public partial class TagController : ControllerBase
    {
        private ITagService _tags;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public TagController(
			ITagService tags

		)
        {
			_tags = tags;
        }

		/// <summary>
		/// GET /v1/tag/2/
		/// Returns the tag data for a single tag.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Tag> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _tags.Get(context, id);
			return await Events.TagLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/tag/2/
		/// Deletes an tag
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _tags.Get(context, id);
			result = await Events.TagDelete.Dispatch(context, result, Response);

			if (result == null || !await _tags.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/tag/list
		/// Lists all tags available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Tag>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/tag/list
		/// Lists filtered tags available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Tag>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Tag>(filters);

			filter = await Events.TagList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _tags.List(context, filter);
			return new Set<Tag>() { Results = results };
		}

		/// <summary>
		/// POST /v1/tag/
		/// Creates a new tag. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Tag> Create([FromBody] TagAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var tag = new Tag
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, tag))
			{
				return null;
			}

			form = await Events.TagCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			tag = await _tags.Create(context, form.Result);

			if (tag == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return tag;
        }

		/// <summary>
		/// POST /v1/tag/1/
		/// Updates a tag with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Tag> Update([FromRoute] int id, [FromBody] TagAutoForm form)
		{
			var context = Request.GetContext();

			var tag = await _tags.Get(context, id);
			
			if (!ModelState.Setup(form, tag)) {
				return null;
			}

			form = await Events.TagUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			tag = await _tags.Update(context, form.Result);

			if (tag == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return tag;
		}
		
    }

}
