using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using System;
using System.Collections.Generic;
using Api.Users;
using Newtonsoft.Json.Linq;
using Api.Messages;
using System.Linq;

namespace Api.Chats
{
    /// <summary>Handles chat endpoints.</summary>
    [Route("v1/chat")]
	public partial class ChatController : AutoController<Chat>
    {
		/// <summary>
		/// Creates or reuses a chat.
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
        [HttpPost("reuse")]
        public async ValueTask Reuse([FromBody] JObject body)
        {
            var context = await Request.GetContext();
			
			// Attempt to find a chat which has userPermits matching exactly what was given.
			var userPermits = body["userPermits"] as JArray;

			IEnumerable<uint> uPerms = Array.Empty<uint>();

			if (userPermits != null)
			{
				uPerms = userPermits.Select(v => v.Value<uint>());
			}
			
			var chat = await _service.Where("UserPermits=[?]").Bind(uPerms).Last(context);
			
			var msgs = Services.Get<MessageService>();
			
            // Does the chat not exist and are we are creating it if it doesn't exist?
            if(chat == null)
            {
				// - Create the chat
				// - Create the message
				// - Update the chat with whatever else was in the json (such as permitted user mappings)

				chat = await _service.Create(context, new Chat() {
					UserId = context.UserId
				});

				// Create the message:
				var msg = body["message"];

				if (msg != null)
				{
					await msgs.Create(context, new Message()
					{
						ChatId = chat.Id,
						Text = msg.Value<string>()
					});
				}

				// Invoke controller update. This outputs the new chat as well.
				await Update(chat.Id, body);
            }
			else
			{
				// Just add the message to it instead.
				
				var msg = body["message"];
				
				if(msg != null)
				{
					await msgs.Create(context, new Message(){
						ChatId = chat.Id,
						Text = msg.Value<string>()
					});
				}

				// Output the chat, same format as create (with all includes):
				await OutputJson(context, chat, "*");
			}
        }
    }
}