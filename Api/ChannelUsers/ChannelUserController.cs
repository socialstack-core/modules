using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Channels;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.ChannelUsers
{
    /// <summary>
    /// Handles channelUser endpoints.
    /// </summary>

    [Route("v1/channel/user")]
	[ApiController]
	public partial class ChannelUserController : ControllerBase
    {
        private IChannelUserService _channelUsers;
        private IChannelService _channels;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ChannelUserController(
            IChannelUserService channelUsers,
			IChannelService channels
        )
        {
            _channelUsers = channelUsers;
            _channels = channels;
        }

		/// <summary>
		/// GET /v1/channel/user/2/
		/// Returns the channel user data for a single channelUser.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ChannelUser> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _channelUsers.Get(context, id);
			return await Events.ChannelUserLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/channel/user/2/
		/// Deletes a channel user
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _channelUsers.Get(context, id);
			result = await Events.ChannelUserDelete.Dispatch(context, result, Response);

			if (result == null || !await _channelUsers.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/channel/user/list
		/// Lists all channel users available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<ChannelUser>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/channel/user/list
		/// Lists filtered channel users available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<ChannelUser>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<ChannelUser>(filters);

			filter = await Events.ChannelUserList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _channelUsers.List(context, filter);
			return new Set<ChannelUser>() { Results = results };
		}

		/// <summary>
		/// POST /v1/channel/user/
		/// Creates a new channelUser. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<ChannelUser> Create([FromBody] ChannelUserAutoForm form)
		{
			var context = Request.GetContext();

			// Get the channel:
			var channel = await _channels.Get(context, form.ChannelId);
			
			if(channel == null){
				return null;
			}
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var channelUser = new ChannelUser
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, channelUser))
			{
				return null;
			}

			form = await Events.ChannelUserCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			channelUser = await _channelUsers.Create(context, form.Result);

			if (channelUser == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return channelUser;
        }

		/// <summary>
		/// POST /v1/channel/user/1/
		/// Creates a new channelUser. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<ChannelUser> Update([FromRoute] int id, [FromBody] ChannelUserAutoForm form)
		{
			var context = Request.GetContext();

			var channelUser = await _channelUsers.Get(context, id);
			
			if (!ModelState.Setup(form, channelUser)) {
				return null;
			}

			form = await Events.ChannelUserUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			channelUser = await _channelUsers.Update(context, form.Result);

			if (channelUser == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return channelUser;
		}

	}

}
