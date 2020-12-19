using Api.Database;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.LiveSupportChats;
using Api.Startup;
using System;

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
		private LiveSupportChatService _liveChat;
		
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

				// If reply to is 1, this is the one that sets the name of the user, so let's grab it.
				if(message.InReplyTo == 1)
                {
					if(_liveChat == null)
                    {
						_liveChat = Services.Get<LiveSupportChatService>();
                    }

					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);
					chat.FullName = message.Message;
					chat = await _liveChat.Update(ctx, chat);
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

			Events.LiveSupportChat.BeforeUpdate.AddEventListener(async (Context ctx, LiveSupportChat chat) =>
			{
				// If we are closing out a support chat by nullifying its EnteredQueue and AssignedToUserId
				// we need to send them a specified chatbot decision:
				// TODO: this needs to be made dynamic in future projects. 
				if (chat == null || ctx == null)
                {
					return null;
                }

				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}
				// Let's load the chat so we can see its current state.
				var currChat = await _liveChat.Get(ctx, chat.Id);

				// Now let's compare, if currChat has both assignedToUser set and enteredQueueUtc, and the new chat doesn't, we need to send message to the user to give control to the bot.
				if(chat.AssignedToUserId == null && chat.EnteredQueueUtc == null && currChat.AssignedToUserId != null && currChat.EnteredQueueUtc != null)
				{
					var alsoSendMessage = await Get(ctx, 11);
					await Task.Delay(2000);
					await SendChatBotMessage(ctx, chat.Id, alsoSendMessage);
				}

				return chat;

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
			// Add an artificial delay to make sure sorts are ok:
			var time = DateTime.UtcNow.AddSeconds(1);
			var context = new Context()
			{
				UserId = 0
			};

			// Check the message text for key words such as {first}
			var returnText = dec.MessageText;

			if (dec.MessageText.Contains("{first}"))
            {
				var returnTextParts = dec.MessageText.Split("{first}");

				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chat = await _liveChat.Get(ctx, chatId);

				var first = chat.FullName.Split(" ")[0];

				returnText = returnTextParts[0] + first + returnTextParts[1];
			}

			if (dec.MessageText.Contains("{queue}"))
            {
				// Let's get the current queue count
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chats = await _liveChat.List(ctx, new Filter<LiveSupportChat>().Not().Equals("EnteredQueueUtc", null).And().Equals("AssignedToUserId", null));

				var queuePosition = chats.Count + 1;

				returnText = returnText.Replace("{queue}", queuePosition.ToString());

			}

			await _liveChatMessages.Create(context, new LiveSupportMessage(){
				LiveSupportChatId = chatId,
				Message = returnText,
				MessageType = dec.MessageType,
				FromSupport = true,
				ReplyTo = dec.ReplyToOverrideId.HasValue ? dec.ReplyToOverrideId.Value : dec.Id,
				PayloadJson = dec.PayloadJson,
				EditedUtc = time,
				CreatedUtc = time
			});

			// Excellent! Before resolving here, let's make sure this decision doesn't have an also send.
			if (dec.AlsoSend.HasValue)
            {
				var alsoSendMessage = await Get(ctx, dec.AlsoSend.Value);
				await Task.Delay(2000);
				await SendChatBotMessage(ctx, chatId, alsoSendMessage);
            }

			if (dec.MessageType == 2)
			{
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chat = await _liveChat.Get(ctx, chatId);
				chat.EnteredQueueUtc = DateTime.Now;
				chat = await _liveChat.Update(ctx, chat);
			}
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
