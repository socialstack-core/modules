using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Users;
using System;
using Api.Startup;

namespace Api.PrivateChats
{
	/// <summary>
	/// Handles privateChats.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PrivateChatService : AutoService<PrivateChat>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PrivateChatService(UserService _users) : base(Events.PrivateChat)
        {
			PrivateChatMessageService messageService = null;
			
			Events.PrivateChat.AfterCreate.AddEventListener(async (Context context, PrivateChat chat) =>
			{
				if (chat == null)
				{
					return null;
				}
				
				// If there's a message, create it as well now:
				if(!string.IsNullOrEmpty(chat.Message))
				{
					if(messageService == null)
					{
						messageService = Services.Get<PrivateChatMessageService>();
					}
					
					await messageService.Create(context, new PrivateChatMessage(){
						Message = chat.Message,
						PrivateChatId = chat.Id,
						UserId = context.UserId
					});
				}
				
				return chat;
			}, 5);
			
		}
		
	}
    
}
