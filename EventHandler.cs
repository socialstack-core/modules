using Api.Contexts;
using Api.Eventing;
using Api.Users;
using Api.Startup;
using Api.WebSockets;
using Microsoft.Extensions.Configuration;
using Api.Configuration;


namespace Api.LiveSupportChats
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[EventListener]
	public partial class LiveSupportChatEventHandler
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LiveSupportChatEventHandler()
		{
			LiveSupportMessageService messages = null;
			var supportConfig = AppSettings.GetSection("LiveSupportChat").Get<LiveSupportChatConfig>();
			
			if(supportConfig == null)
			{
				supportConfig = new LiveSupportChatConfig();
			}
			
            Events.LiveSupportChat.AfterCreate.AddEventListener(async (Context context, LiveSupportChat liveChat) =>
			{
				if(liveChat == null)
				{
					return null;
				}
				
				// Add initial welcome message if there is one:
				if(!string.IsNullOrEmpty(supportConfig.WelcomeMessage))
				{
					if(messages == null)
					{
						messages = Services.Get<LiveSupportMessageService>();
					}
					
					await messages.Create(context, new LiveSupportMessage(){
						
						LiveSupportChatId = liveChat.Id,
						Message = supportConfig.WelcomeMessage,
						ChatCreatorUserId = liveChat.UserId,
						FromSupport = true
						
					});
				}
				
                return liveChat;
			});
		}

	}

}
