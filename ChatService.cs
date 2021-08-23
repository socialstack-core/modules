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

			Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) => 
			{

				var block = new IDCollector<uint>();
				var rand = new System.Random();

				for (int i = 0; i < 300; i++)
                {
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					block.AddSorted((uint)i+1);
					//block.AddSorted((uint)rand.Next(1, 130));
				}

				block.Eliminate(11, true);



				
				//var chat5 = await Get(ctx, 5, DataOptions.IgnorePermissions);
				/*var chat55 = await Get(ctx, 55, DataOptions.IgnorePermissions);
				var chat56 = await Get(ctx, 56, DataOptions.IgnorePermissions);
				
				*/
				//var filter1 = Where("UserPermits=[?]").Bind(new uint[] { 1, 3 });
				//filter1.FirstCollector = await filter1.RentAndCollect(ctx, this);
				//var eval1 = filter1.Match(ctx, chat5, false);
				/*var eval2 = filter1.Match(ctx, chat55, filter1);
				var eval3 = filter1.Match(ctx, chat56, filter1);

				/*
				var filter2 = Where("UserPermits contains [?]").Bind(new uint[] { 1 });
				filter2.FirstACollector = await filter2.RentAndCollect(ctx, this);
				var eval2 = filter2.Match(ctx, chat1, filter2);
				

				var filter3 = Where("UserPermits!=[?]").Bind(new uint[] { 1, 3 });
				filter3.FirstACollector = await filter3.RentAndCollect(ctx, this);
				var eval3 = filter3.Match(ctx, chat1, filter3);
				var filter6 = Where("UserPermits containsNone [?]").Bind(new uint[] { 1 });
				filter6.FirstACollector = await filter6.RentAndCollect(ctx, this);
				var eval6 = filter6.Match(ctx, chat1, filter6);
			

				var filter5 = Where("UserPermits containsAll [?]").Bind(new uint[] { 1 });
				filter5.FirstACollector = await filter5.RentAndCollect(ctx, this);
				var eval5 = filter5.Match(ctx, chat1, filter5);

				var filter4 = Where("UserPermits containsAny [?]").Bind(new uint[] { 1 });
				filter4.FirstACollector = await filter4.RentAndCollect(ctx, this);
				var eval4 = filter4.Match(ctx, chat1, filter4);

				var test = "test";
				*/

				return src;
			});
			
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
