using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Api.ProfanityFilter;
using Api.Startup;

namespace Api.LiveChats
{
	/// <summary>
	/// Handles live stream chat messages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LiveChatMessageService : AutoService<LiveChatMessage>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LiveChatMessageService(ProfanityFilterService filter) : base(Events.LiveChatMessage)
        {
			InstallAdminPages(null, null, new string[] { "id", "message" });

			Events.LiveChatMessage.BeforeCreate.AddEventListener((Context context, LiveChatMessage chatMessage) =>
			{
				if (chatMessage == null)
				{
					return new ValueTask<LiveChatMessage>(chatMessage);
				}
				
				// Check for profanity:
				var measurement = filter.Measure(chatMessage.Message);
				
				if(measurement != 0)
				{
					chatMessage.ProfanityWeight = measurement;
				}
				
				return new ValueTask<LiveChatMessage>(chatMessage);
			});
			
			Events.LiveChatMessage.BeforeSettable.AddEventListener((Context context, JsonField<LiveChatMessage, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<LiveChatMessage, uint>>(field);
				}

				if(field.ForRole != Roles.Admin && field.ForRole != Roles.Developer){
					
					if(field.Name == "ProfanityWeight"){
						// Not settable by any other roles.
						field = null;
					}
					
				}
				
				return new ValueTask<JsonField<LiveChatMessage, uint>>(field);
			});
			
        }
    }

}
