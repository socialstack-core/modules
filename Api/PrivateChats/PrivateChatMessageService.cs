using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PrivateChats
{
	/// <summary>
	/// Handles privateChatMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PrivateChatMessageService : AutoService<PrivateChatMessage>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PrivateChatMessageService(PrivateChatService _chats) : base(Events.PrivateChatMessage)
        {
			
			Events.PrivateChatMessage.BeforeCreate.AddEventListener(async (Context context, PrivateChatMessage msg) => {

				if (msg.PrivateChatId == 0)
				{
					// Chat ID must be set.
					return null;
				}

				// User able to msg this channel?
				// Get the channel:
				/*
				var channel = await _chats.Get(context, msg.PrivateChatId);
					
				if(channel == null){
					// Doesn't exist.
					return null;
				}*/

				// Must be permitted to access the channel.
#warning todo check context has channel access
				/*
				if(
					false
				) {
					// Nope
					return null;
				}
				*/
					
				await _chats.Update(context, msg.PrivateChatId, (Context c, PrivateChat channel, PrivateChat originalChannel) => {
					// Update the channel with the number of messages in it:
					channel.MessageCount++;
				});

				// Ok:
				return msg;
			});
			
		}
	}
    
}
