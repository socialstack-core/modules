using Api.Contexts;
using Api.Eventing;
using Api.Messages;
using Api.Startup;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Chats
{
	/// <summary>
	/// Handles chats.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChatService : AutoService<Chat>
    {
		private readonly MessageService _messages;
		private ComposableChangeField _chatFieldChanges;
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChatService(MessageService messages) : base(Events.Chat)
        {
			_messages = messages;
			_chatFieldChanges = GetChangeField("LastMessageId").And("MessageCount");
			
			Events.Message.AfterCreate.AddEventListener(async (Context context, Message message) =>
			{
				if(context == null || message == null)
                {
					return null;
                }

				if(message.ChatId > 0)
                {
					// Let's attempt to update the chat.
					var chat = await Update(context, message.ChatId, (Context ctx, Chat cht) =>
					{
						// update last message id and message count.
						cht.LastMessageId = message.Id;
						cht.MessageCount = cht.MessageCount + 1;
						cht.MarkChanged(_chatFieldChanges);
					});
                }

				return message;
			});
		}
	}
    
}
