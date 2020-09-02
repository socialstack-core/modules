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

				if (msg.PrivateChatId == 0)
				{
					// Chat ID must be set.
					return null;
				}

				// User able to msg this channel?
				// Get the channel:
				var channel = await _chats.Get(context, msg.PrivateChatId);
					
				if(channel == null){
					// Doesn't exist.
					return null;
				}

				// Context must represent either the channel source, or the channel target.
				if(
					!context.HasContent(channel.SourceContentType, channel.SourceContentId) &&
					!context.HasContent(channel.TargetContentType, channel.TargetContentId)
				) {
					// Nope
					return null;
				}
				
				// Clone in the src/ target:
				msg.SourceContentType = channel.SourceContentType;
				msg.TargetContentType = channel.TargetContentType;
				msg.TargetContentId = channel.TargetContentId;
				msg.SourceContentId = channel.SourceContentId;
				
				// Update the channel with the number of messages in it:
				channel.MessageCount++;
					
				await _chats.Update(context, channel);

				// Ok:
				return msg;
			});
			
		}
	}
    
}
