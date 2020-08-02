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
				// First we'll collect all their IDs so we can do a single bulk lookup.
				// ASSUMPTION: The list is not excessively long!
				// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
				// (applies to at least categories/ tags).

#warning todo this needs to be modified to support companies (eBay) as well

				var uniqueUsers = new Dictionary<int, UserProfile>();

				for (var i = 0; i < list.Count; i++)
				{
					var entry = list[i];

					if (entry == null)
					{
						continue;
					}

					// Add to content lookup so we can map the tags to it shortly:
					var creatorId = entry.TargetContentId;

					if (creatorId != 0)
					{
						uniqueUsers[creatorId] = null;
					}
				}

				if (uniqueUsers.Count == 0)
				{
					// Nothing to do - just return here:
					return list;
				}

				// Create the filter and run the query now:
				var userIds = new object[uniqueUsers.Count];
				var index = 0;
				foreach (var kvp in uniqueUsers)
				{
					userIds[index++] = kvp.Key;
				}

				var filter = new Filter<User>();
				filter.EqualsSet("Id", userIds);

				// Use the regular list method here:
				var allUsers = await _users.List(context, filter);

				foreach (var user in allUsers)
				{
					// Get as the public profile and hook up the mapping:
					var profile = await _users.GetProfile(context, user);
					uniqueUsers[user.Id] = profile;
				}

				for (var i = 0; i < list.Count; i++)
				{
					// Get as IHaveCreatorUser objects (must be valid because of the above check):
					var entry = list[i];

					if (entry == null)
					{
						continue;
					}

					// Add to content lookup so we can map the tags to it shortly:
					var creatorId = entry.TargetContentId;

					// Note that this user object might be null.
					var userProfile = creatorId == 0 ? null : uniqueUsers[creatorId];

					// Get it as the public profile object next:
					entry.Target = userProfile;
				}

				return list;
			}, 5);
			
		}
	}
    
}
