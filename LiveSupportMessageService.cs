using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.LiveSupportChats
{
	/// <summary>
	/// Handles liveSupportMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LiveSupportMessageService : AutoService<LiveSupportMessage>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LiveSupportMessageService(LiveSupportChatService chats) : base(Events.LiveSupportMessage)
        {
			
			Events.LiveSupportMessage.BeforeCreate.AddEventListener(async (Context context, LiveSupportMessage message) => {
				
				if(message == null)
				{
					return message;
				}
				
				// Get the chat:
				var chat = await chats.Get(context, message.LiveSupportChatId);
				
				if(chat == null)
				{
					// Reject - chat does not exist.
					return null;
				}
				
				// Future todo: check if context user is permitted to add messages into this chat.
				
				// Set creator ID:
				message.ChatCreatorUserId = chat.UserId;
				
				return message;
				
			}, 10);
			
		}
	}
    
}
