using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Galleries
{
    /// <summary>
    /// Handles gallery endpoints.
    /// </summary>

    [Route("v1/gallery")]
	[ApiController]
	public partial class GalleryController : ControllerBase
    {
        private IGalleryService _galleries;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public GalleryController(
			IGalleryService galleries

		)
        {
			_galleries = galleries;
        }

		/// <summary>
		/// GET /v1/gallery/2/
		/// Returns the gallery data for a single gallery.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Gallery> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _galleries.Get(context, id);
			return await Events.GalleryLoad.Dispatch(context, result, Response);
		}
		
		/// <summary>
		/// DELETE /v1/gallery/2/
		/// Deletes a gallery
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _galleries.Get(context, id);
			result = await Events.GalleryDelete.Dispatch(context, result, Response);

			if (result == null || !await _galleries.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/gallery/list
		/// Lists all galleries available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Gallery>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/gallery/list
		/// Lists filtered galleries available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Gallery>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Gallery>(filters);

			filter = await Events.GalleryList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _galleries.List(context, filter);
			return new Set<Gallery>() { Results = results };
		}
		
		/// <summary>
		/// POST /v1/gallery/
		/// Creates a new gallery. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Gallery> Create([FromBody] GalleryAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var gallery = new Gallery
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, gallery))
			{
				return null;
			}

			form = await Events.GalleryCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			gallery = await _galleries.Create(context, form.Result);

			if (gallery == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return gallery;
        }

		/// <summary>
		/// POST /v1/gallery/1/
		/// Updates a gallery with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Gallery> Update([FromRoute] int id, [FromBody] GalleryAutoForm form)
		{
			var context = Request.GetContext();

			var gallery = await _galleries.Get(context, id);
			
			if (!ModelState.Setup(form, gallery)) {
				return null;
			}

			form = await Events.GalleryUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			gallery = await _galleries.Update(context, form.Result);

			if (gallery == null)
			{
				Response.StatusCode = 500;
				return null;
			}

			return gallery;
		}

    }

}
