using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.LiveSupportChats;
using Api.Startup;

namespace Api.ChatBotSimple
{
	/// <summary>
	/// Handles chatBotDecisions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChatBotDecisionService : AutoService<ChatBotDecision>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChatBotDecisionService(LiveSupportMessageService liveChatMessages) : base(Events.ChatBotDecision)
        {
			InstallAdminPages("ChatBot Decisions", "fa:fa-rocket", new string[] { "id", "message" });
			
			Events.LiveSupportMessage.AfterCreate.AddEventListener(async (Context ctx, LiveSupportMessage message) => {

				if (message == null)
				{
					return message;
				}
				
				// Todo: Use the CacheLoaded response to lookup responses from the CMS.
				// Ex theDictionary.TryGetValue(message.InReplyTo, out List<ChatBotDecision> possibleDecisionsHere);
				
				if(message.Message == "ama")
				{
					// Somebody put the phrase 'ama' into the text box.
					// We'll now respond with a sample question:
					await liveChatMessages.Create(new Context(){
						UserId = 0
					}, new LiveSupportMessage(){
						LiveSupportChatId = message.LiveSupportChatId,
						Message = "Do you like to do the cha-cha?",
						MessageType = 1,
						FromSupport = true,
						ReplyTo = 1, //  This can be whatever you want (e.g. the ID of a ChatBotDecision object)
						PayloadJson = "{\"module\": \"UI/LiveSupport/MultiSelect\", \"data\": {\"answers\": [\"Not really no!\", \"Sure but only on a friday night\", \"Every day!\"]}}"
					});
					
				}else if(message.InReplyTo == 1 && message.Message == "Every day!")
				{
					// Somebody replied to a message with ReplyTo=1 and specifically selected the "Every day!" option. 
					// Continue the flow from there:
					await liveChatMessages.Create(new Context(){
						UserId = 0
					}, new LiveSupportMessage(){
						LiveSupportChatId = message.LiveSupportChatId,
						Message = "Gee, me too!",
						FromSupport = true
					});
				}

				return message;
			});
			
			Cache(new CacheConfig<ChatBotDecision>(){
				Preload = true,
				OnCacheLoaded = () => {

					// Called when the cache has everything in it.
					Task.Run(async () => {

						await UpdateLookup();

					});

				}
			});
		}

		private async Task UpdateLookup()
		{
			// Todo: Build a lookup for InReplyTo values, each containing the list of (optional) specific responses.
			// Ex Dictionary<int, List<ChatBotDecision>>
			var everything = await List(new Context(), new Filter<ChatBotDecision>());

			// foreach in everything -> add to dictionary
		}
	}
    
}
