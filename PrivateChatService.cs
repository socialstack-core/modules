using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Users;
using System;

namespace Api.PrivateChats
{
	/// <summary>
	/// Handles privateChats.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PrivateChatService : AutoService<PrivateChat>, IPrivateChatService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PrivateChatService(IUserService _users) : base(Events.PrivateChat)
        {

			Events.PrivateChat.AfterLoad.AddEventListener(async (Context context, PrivateChat chat) =>
			{
				if (chat == null)
				{
					return null;
				}

				if (chat.TargetContentId != 0)
				{
					// Get the target info:
					chat.Target = await Content.Get(context, chat.TargetContentType, chat.TargetContentId);
				}

				if (chat.SourceContentId != 0)
				{
					// Get the source info:
					chat.Source = await Content.Get(context, chat.SourceContentType, chat.SourceContentId);
				}

				return chat;
			}, 5);
			
			Events.PrivateChat.AfterUpdate.AddEventListener(async (Context context, PrivateChat chat) =>
			{
				if (chat == null)
				{
					return null;
				}

				if (chat.TargetContentId != 0)
				{
					// Get the target info:
					chat.Target = await Content.Get(context, chat.TargetContentType, chat.TargetContentId);
				}

				if (chat.SourceContentId != 0)
				{
					// Get the source info:
					chat.Source = await Content.Get(context, chat.SourceContentType, chat.SourceContentId);
				}

				return chat;
			}, 5);
			
			Events.PrivateChat.AfterCreate.AddEventListener(async (Context context, PrivateChat chat) =>
			{
				if (chat == null)
				{
					return null;
				}

				if (chat.TargetContentId != 0)
				{
					// Get the target info:
					chat.Target = await Content.Get(context, chat.TargetContentType, chat.TargetContentId);
				}

				if (chat.SourceContentId != 0)
				{
					// Get the source info:
					chat.Source = await Content.Get(context, chat.SourceContentType, chat.SourceContentId);
				}

				return chat;
			}, 5);

			Events.PrivateChat.BeforeCreate.AddEventListener((Context context, PrivateChat chat) => {

				// Permitted to create this chat for the named source if this context has that source.
				if (chat == null)
				{
					return Task.FromResult(chat);
				}

				if (chat.SourceContentType == 0 && chat.SourceContentId == 0)
				{
					// It's not set - default to being from the contextual user.
					chat.SourceContentType = ContentTypes.GetId(typeof(User));
					chat.SourceContentId = context.UserId;
				}
				else if (!context.HasContent(chat.SourceContentType, chat.SourceContentId))
				{
					// The context does not have this source.
					// For example, user tried to send as company Y, but they aren't authenticated as company Y.
					// Aka, go away!
					return Task.FromResult((PrivateChat)null);
				}

				return Task.FromResult(chat);
			});

			Events.PrivateChat.AfterList.AddEventListener(async (Context context, List<PrivateChat> list) =>
			{
				if (list == null)
				{
					return list;
				}

				// Can be mixed content (e.g. chats between users/ companies etc) so we'll use the Content.ApplyMixed helper:
				await Content.ApplyMixed(
					context,
					list,
					src =>
					{
						// Never invoked with null.
						var privateChat = (PrivateChat)src;
						return new ContentTypeAndId(privateChat.TargetContentType, privateChat.TargetContentId);
					},
					(object src, object content) =>
					{
						var privateChat = (PrivateChat)src;
						privateChat.Target = content;
					}
				);

				await Content.ApplyMixed(
					context,
					list,
					src =>
					{
						// Never invoked with null.
						var privateChat = (PrivateChat)src;
						return new ContentTypeAndId(privateChat.SourceContentType, privateChat.SourceContentId);
					},
					(object src, object content) =>
					{
						var privateChat = (PrivateChat)src;
						privateChat.Source = content;
					}
				);
				
				return list;
			});
		}
	}
    
}
