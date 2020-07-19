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
	public partial class PrivateChatMessageService : AutoService<PrivateChatMessage>, IPrivateChatMessageService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PrivateChatMessageService(IPrivateChatService _chats) : base(Events.PrivateChatMessage)
        {
			
			Events.PrivateChatMessage.BeforeCreate.AddEventListener(async (Context context, PrivateChatMessage msg) => {
				
				// User able to msg this channel?
				// Get the channel:
				var channel = await _chats.Get(context, msg.PrivateChatId);
				
				if(channel == null){
					// Doesn't exist.
					return null;
				}
				
				if(channel.UserId != context.UserId && channel.WithUserId != context.UserId){
					// Nope
					return null;
				}
				
				// Set the with user ref:
				msg.WithUserId = (context.UserId == channel.UserId) ? channel.WithUserId : channel.UserId;
				
				// Update the channel with the number of messages in it:
				channel.MessageCount++;
				
				await _chats.Update(context, channel);
				
				return msg;
			});
			
		}
	}
    
}
