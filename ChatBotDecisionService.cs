using Api.Database;
using System.Threading.Tasks;
using System.Collections;
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
		private Dictionary<int, List<ChatBotDecision>> _inReplyToMap;
		private LiveSupportMessageService _liveChatMessages;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChatBotDecisionService(LiveSupportMessageService liveChatMessages) : base(Events.ChatBotDecision)
        {
			_liveChatMessages = liveChatMessages;
			
			InstallAdminPages("ChatBot Decisions", "fa:fa-rocket", new string[] { "id", "messageText" });
			
			Events.LiveSupportChat.AfterCreate.AddEventListener(async(Context ctx, LiveSupportChat chat) => {
				
				if (chat == null || _inReplyToMap == null)
				{
					return chat;
				}
				
				List<ChatBotDecision> list = null;
				if(_inReplyToMap.TryGetValue(0, out list) && list.Count > 0)
				{
					// In this case we can only use the first entry.
					await SendChatBotMessage(ctx, chat.Id, list[0]);
				}
				
				return chat;
			});
			
			Events.LiveSupportMessage.AfterCreate.AddEventListener(async (Context ctx, LiveSupportMessage message) => {

				if (message == null || _inReplyToMap == null || message.InReplyTo == 0)
				{
					return message;
				}
				
				// message.LiveSupportChatId
				List<ChatBotDecision> list = null;
				if(_inReplyToMap.TryGetValue(message.InReplyTo, out list) && list.Count > 0)
				{
					// Chatbot has something to say in response to this message.
					// Find the right response based on what the user sent. 
					ChatBotDecision noneResponse = null;
					ChatBotDecision dec = null;
					
					foreach(var entry in list)
					{
						if(string.IsNullOrEmpty(entry.AnswerProvided))
						{
							noneResponse = entry;
						}
						else if(entry.AnswerProvided == message.Message)
						{
							// User selected some previous response and sent it to the chatbot
							dec = entry;
							break;
						}
					}
					
					if(dec == null)
					{
						dec = noneResponse;
					}
					
					if(dec != null)
					{
						// Using this one.
						await SendChatBotMessage(ctx, message.LiveSupportChatId, dec);
					}
					
				}
				
				return message;
			});
			
			Events.ChatBotDecision.AfterCreate.AddEventListener((Context ctx, ChatBotDecision dec) => {
				AddToMap(dec);
				return new ValueTask<ChatBotDecision>(dec);
			});
			
			Events.ChatBotDecision.AfterUpdate.AddEventListener(async (Context ctx, ChatBotDecision dec) => {
				
				// Bulky - would be nicer to actually update only the entry that changed!
				await UpdateLookup();
				
				return dec;
			});
			
			Events.ChatBotDecision.AfterDelete.AddEventListener(async (Context ctx, ChatBotDecision dec) => {
				
				// Bulky - would be nicer to actually update only the entry that changed!
				await UpdateLookup();
				
				return dec;
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
		
		private async Task SendChatBotMessage(Context ctx, int chatId, ChatBotDecision dec)
		{
			await _liveChatMessages.Create(new Context(){
				UserId = 0
			}, new LiveSupportMessage(){
				LiveSupportChatId = chatId,
				Message = dec.MessageText,
				MessageType = dec.MessageType,
				FromSupport = true,
				ReplyTo = dec.Id,
				PayloadJson = dec.PayloadJson
			});
		}
		
		private void AddToMap(ChatBotDecision dec)
		{
			if(_inReplyToMap.TryGetValue(dec.InReplyTo, out List<ChatBotDecision> list))
			{
				// Add it:
				list.Add(dec);
			}
			else
			{
				list = new List<ChatBotDecision>();
				_inReplyToMap[dec.InReplyTo] = list;
				list.Add(dec);
			}
		}
		
		private async Task UpdateLookup()
		{
			// Build a lookup for InReplyTo values, each containing the list of (optional) specific responses:
			// InReplyTo 0 represents the user opening a chat.
			var everything = await List(new Context(), new Filter<ChatBotDecision>());

			_inReplyToMap = new Dictionary<int, List<ChatBotDecision>>();

			foreach(var dec in everything)
			{
				AddToMap(dec);
			}
		}
	}
    
}
