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

namespace Api.ChannelMessages
{
    /// <summary>
    /// Handles message endpoints.
    /// </summary>

    [Route("v1/channel/message")]
	[ApiController]
	public partial class ChannelMessageController : ControllerBase
    {
        private IChannelMessageService _messages;
        private IChannelService _channels;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ChannelMessageController(
            IChannelMessageService messages,
			IChannelService channels
        )
        {
            _messages = messages;
            _channels = channels;
        }

		/// <summary>
		/// GET /v1/channel/message/2/
		/// Returns the message data for a single message.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ChannelMessage> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _messages.Get(context, id);
			return await Events.ChannelMessageLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/channel/message/2/
		/// Deletes an message
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _messages.Get(context, id);
			result = await Events.ChannelMessageDelete.Dispatch(context, result, Response);

			if (result == null || !await _messages.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/channel/message/list
		/// Lists all messages available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<ChannelMessage>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/channel/message/list
		/// Lists filtered messages available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<ChannelMessage>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<ChannelMessage>(filters);

			filter = await Events.ChannelMessageList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _messages.List(context, filter);
			return new Set<ChannelMessage>() { Results = results };
		}

		/// <summary>
		/// POST /v1/channel/message/
		/// Creates a new message. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<ChannelMessage> Create([FromBody] ChannelMessageAutoForm form)
		{
			var context = Request.GetContext();

			// Get the channel so we can grab the board ID:
			var channel = await _channels.Get(context, form.ChannelId);
			
			if(channel == null){
				// Nope
				return null;
			}
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var message = new ChannelMessage
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, message))
			{
				return null;
			}

			form = await Events.ChannelMessageCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			message = await _messages.Create(context, form.Result);

			if (message == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return message;
        }

		/// <summary>
		/// POST /v1/channel/message/1/
		/// Creates a new message. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<ChannelMessage> Update([FromRoute] int id, [FromBody] ChannelMessageAutoForm form)
		{
			var context = Request.GetContext();

			var message = await _messages.Get(context, id);
			
			if (!ModelState.Setup(form, message)) {
				return null;
			}

			form = await Events.ChannelMessageUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			message = await _messages.Update(context, form.Result);

			if (message == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return message;
		}

	}

}
