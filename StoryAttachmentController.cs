using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Users;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.StoryAttachments
{
    /// <summary>
    /// Handles story attachment endpoints. These attachments are e.g. images attached to a feed story or a message in a chat channel.
    /// </summary>

    [Route("v1/story/attachment")]
	[ApiController]
	public partial class StoryAttachmentController : ControllerBase
    {
        private IStoryAttachmentService _storyAttachments;
        private IUserService _users;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public StoryAttachmentController(
            IStoryAttachmentService storyAttachments,
			IUserService users
        )
        {
            _storyAttachments = storyAttachments;
            _users = users;
        }

		/// <summary>
		/// GET /v1/story/attachment/2/
		/// Returns the storyAttachment data for a single story attachment.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<StoryAttachment> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _storyAttachments.Get(context, id);
			return await Events.StoryAttachmentLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/story/attachment/2/
		/// Deletes a story attachment
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _storyAttachments.Get(context, id);
			result = await Events.StoryAttachmentDelete.Dispatch(context, result, Response);

			if (result == null || !await _storyAttachments.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/story/attachment/list
		/// Lists all story attachments available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<StoryAttachment>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/story/attachment/list
		/// Lists filtered story attachments available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<StoryAttachment>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<StoryAttachment>(filters);

			filter = await Events.StoryAttachmentList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _storyAttachments.List(context, filter);
			return new Set<StoryAttachment>() { Results = results };
		}

		/// <summary>
		/// POST /v1/story/attachment/
		/// Creates a new story attachment. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<StoryAttachment> Create([FromBody] StoryAttachmentAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var storyAttachment = new StoryAttachment
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, storyAttachment))
			{
				return null;
			}

			form = await Events.StoryAttachmentCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			storyAttachment = await _storyAttachments.Create(context, form.Result);

			if (storyAttachment == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return storyAttachment;
        }

		/// <summary>
		/// POST /v1/story/attachment/1/
		/// Creates a new story attachment. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<StoryAttachment> Update([FromRoute] int id, [FromBody] StoryAttachmentAutoForm form)
		{
			var context = Request.GetContext();

			var storyAttachment = await _storyAttachments.Get(context, id);
			
			if (!ModelState.Setup(form, storyAttachment)) {
				return null;
			}

			form = await Events.StoryAttachmentUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			storyAttachment = await _storyAttachments.Update(context, form.Result);

			if (storyAttachment == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return storyAttachment;
		}

	}

}
