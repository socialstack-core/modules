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


namespace Api.FeedStories
{
    /// <summary>
    /// Handles feedStory endpoints.
    /// </summary>

    [Route("v1/feed/story")]
	[ApiController]
	public partial class FeedStoryController : ControllerBase
    {
        private IFeedStoryService _feedStories;
        private IUserService _users;
		
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FeedStoryController(
            IFeedStoryService feedStories,
			IUserService users
        )
        {
            _feedStories = feedStories;
            _users = users;
        }

		/// <summary>
		/// GET /v1/feed/story/2/
		/// Returns the feed story data for a single feedStory.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<FeedStory> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _feedStories.Get(context, id);
			return await Events.FeedStoryLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/feed/story/2/
		/// Deletes a feed story
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _feedStories.Get(context, id);
			result = await Events.FeedStoryDelete.Dispatch(context, result, Response);

			if (result == null || !await _feedStories.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/feed/story/list
		/// Lists all feed stories available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<FeedStory>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/feed/story/list
		/// Lists filtered feed stories available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<FeedStory>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<FeedStory>(filters);

			filter = await Events.FeedStoryList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _feedStories.List(context, filter);
			return new Set<FeedStory>() { Results = results };
		}
		
		/// <summary>
		/// POST /v1/feed/story/
		/// Creates a new feed story. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<FeedStory> Create([FromBody] FeedStoryAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var feedStory = new FeedStory
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, feedStory))
			{
				return null;
			}

			form = await Events.FeedStoryCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			feedStory = await _feedStories.Create(context, form.Result);

			if (feedStory == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return feedStory;
        }

		/// <summary>
		/// POST /v1/feed/story/1/
		/// Creates a new feed story. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<FeedStory> Update([FromRoute] int id, [FromBody] FeedStoryAutoForm form)
		{
			var context = Request.GetContext();

			var feedStory = await _feedStories.Get(context, id);
			
			if (!ModelState.Setup(form, feedStory)) {
				return null;
			}

			form = await Events.FeedStoryUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			feedStory = await _feedStories.Update(context, form.Result);

			if (feedStory == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return feedStory;
		}

	}

}
