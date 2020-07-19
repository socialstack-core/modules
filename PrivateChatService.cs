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

				if (chat == null || chat.WithUserId == 0)
				{
					return chat;
				}

				// Get the with user:
				chat.WithUser = await _users.GetProfile(context, chat.WithUserId);

				return chat;
			}, 5);
			
			Events.PrivateChat.AfterUpdate.AddEventListener(async (Context context, PrivateChat chat) =>
			{

				if (chat == null || chat.WithUserId == 0)
				{
					return chat;
				}

				// Get the with user:
				chat.WithUser = await _users.GetProfile(context, chat.WithUserId);

				return chat;
			}, 5);
			
			Events.PrivateChat.AfterCreate.AddEventListener(async (Context context, PrivateChat chat) =>
			{

				if (chat == null || chat.WithUserId == 0)
				{
					return chat;
				}

				// Get the with user:
				chat.WithUser = await _users.GetProfile(context, chat.WithUserId);

				return chat;
			}, 5);
			
			Events.PrivateChat.AfterList.AddEventListener(async (Context context, List<PrivateChat> list) =>
			{
				// First we'll collect all their IDs so we can do a single bulk lookup.
				// ASSUMPTION: The list is not excessively long!
				// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
				// (applies to at least categories/ tags).

				var uniqueUsers = new Dictionary<int, UserProfile>();

				for (var i = 0; i < list.Count; i++)
				{
					var entry = list[i];

					if (entry == null)
					{
						continue;
					}

					// Add to content lookup so we can map the tags to it shortly:
					var creatorId = entry.WithUserId;

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
					var creatorId = entry.WithUserId;

					// Note that this user object might be null.
					var userProfile = creatorId == 0 ? null : uniqueUsers[creatorId];

					// Get it as the public profile object next:
					entry.WithUser = userProfile;
				}

				return list;
			}, 5);
			
		}
	}
    
}
