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

namespace Api.Followers
{
    /// <summary>
    /// Handles follower endpoints.
    /// </summary>

    [Route("v1/follower")]
	[ApiController]
	public partial class FollowerController : ControllerBase
    {
        private IFollowerService _followers;
        private IUserService _users;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FollowerController(
            IFollowerService followers,
			IUserService users
        )
        {
            _followers = followers;
            _users = users;
        }

		/// <summary>
		/// GET /v1/follower/2/
		/// Returns the follower data for a single follower.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Follower> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _followers.Get(context, id);
			return await Events.FollowerLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/follower/2/
		/// Deletes a follower
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _followers.Get(context, id);
			result = await Events.FollowerDelete.Dispatch(context, result, Response);

			if (result == null || !await _followers.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/follower/list
		/// Lists all followers available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Follower>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/follower/list
		/// Lists filtered followers available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Follower>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Follower>(filters);

			filter = await Events.FollowerList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _followers.List(context, filter);
			return new Set<Follower>() { Results = results };
		}

		/// <summary>
		/// POST /v1/follower/
		/// Creates a new follower. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Follower> Create([FromBody] FollowerAutoForm form)
		{
			var context = Request.GetContext();

			// Get the target user:
			var targetUser = await _users.Get(context, form.SubscribedToId);
			
			if(targetUser == null){
				return null;
			}
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var follower = new Follower
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, follower))
			{
				return null;
			}

			form = await Events.FollowerCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			follower = await _followers.Create(context, form.Result);

			if (follower == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return follower;
        }

		/// <summary>
		/// POST /v1/follower/1/
		/// Creates a new follower. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Follower> Update([FromRoute] int id, [FromBody] FollowerAutoForm form)
		{
			var context = Request.GetContext();

			var follower = await _followers.Get(context, id);
			
			if (!ModelState.Setup(form, follower)) {
				return null;
			}

			form = await Events.FollowerUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			follower = await _followers.Update(context, form.Result);

			if (follower == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return follower;
		}

	}

}
