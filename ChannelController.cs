using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Questions;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Channels
{
    /// <summary>
    /// Handles channel endpoints.
    /// </summary>

    [Route("v1/channel")]
	[ApiController]
	public partial class ChannelController : ControllerBase
    {
        private IChannelService _channels;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ChannelController(
            IChannelService channels
        )
        {
            _channels = channels;
        }

		/// <summary>
		/// GET /v1/channel/2/
		/// Returns the channel data for a single channel.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Channel> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _channels.Get(context, id);
			return await Events.ChannelLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/channel/2/
		/// Deletes a channel
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _channels.Get(context, id);
			result = await Events.ChannelDelete.Dispatch(context, result, Response);

			if (result == null || !await _channels.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/channel/list
		/// Lists all channels available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Channel>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/channel/list
		/// Lists filtered channels available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Channel>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Channel>(filters);

			filter = await Events.ChannelList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _channels.List(context, filter);
			return new Set<Channel>() { Results = results };
		}

		/// <summary>
		/// POST /v1/channel/
		/// Creates a new channel. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Channel> Create([FromBody] ChannelAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var channel = new Channel
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, channel))
			{
				return null;
			}

			form = await Events.ChannelCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			channel = await _channels.Create(context, form.Result);

			if (channel == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return channel;
        }

		/// <summary>
		/// POST /v1/channel/1/
		/// Creates a new channel. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Channel> Update([FromRoute] int id, [FromBody] ChannelAutoForm form)
		{
			var context = Request.GetContext();

			var channel = await _channels.Get(context, id);
			
			if (!ModelState.Setup(form, channel)) {
				return null;
			}
			
			form = await Events.ChannelUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			channel = await _channels.Update(context, form.Result);

			if (channel == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return channel;
		}

	}

}
