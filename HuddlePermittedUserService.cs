using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.Linq;
using Api.Users;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddlePermittedUsers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddlePermittedUserService : AutoService<HuddlePermittedUser>, IHuddlePermittedUserService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddlePermittedUserService(IUserService users) : base(Events.HuddlePermittedUser)
        {
			Events.HuddlePermittedUser.AfterLoad.AddEventListener(async (Context context, HuddlePermittedUser permit) =>
			{
				if (permit == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				// Get the permitted user profile:
				if (permit.PermittedUserId != 0)
				{
					permit.PermittedUser = await users.GetProfile(context, permit.PermittedUserId);
				}

				return permit;
			});

			Events.HuddlePermittedUser.AfterUpdate.AddEventListener(async (Context context, HuddlePermittedUser permit) =>
			{
				if (permit == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				// Get the permitted user profile:
				if (permit.PermittedUserId != 0)
				{
					permit.PermittedUser = await users.GetProfile(context, permit.PermittedUserId);
				}

				return permit;
			});

			Events.HuddlePermittedUser.AfterCreate.AddEventListener(async (Context context, HuddlePermittedUser permit) =>
			{
				if (permit == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				// Get the permitted user profile:
				if (permit.PermittedUserId != 0)
				{
					permit.PermittedUser = await users.GetProfile(context, permit.PermittedUserId);
				}

				return permit;
			});

			Events.HuddlePermittedUser.AfterList.AddEventListener(async (Context context, List<HuddlePermittedUser> huddles) =>
			{
				if (huddles == null)
				{
					return null;
				}

				// Get permitted user profiles:
				var userMap = new Dictionary<int, UserProfile>();

				foreach (var huddle in huddles)
				{
					if (huddle == null || huddle.PermittedUserId == 0)
					{
						continue;
					}

					userMap[huddle.PermittedUserId] = null;
				}

				if (userMap.Count != 0)
				{
					var profileSet = await users.ListProfiles(context, new Filter<User>().EqualsSet("Id", userMap.Keys));

					if (profileSet != null)
					{
						foreach (var profile in profileSet)
						{
							userMap[profile.Id] = profile;
						}
					}

					foreach (var huddle in huddles)
					{
						if (huddle == null)
						{
							continue;
						}

						if (userMap.TryGetValue(huddle.PermittedUserId, out UserProfile profile))
						{
							huddle.PermittedUser = profile;
						}
					}
				}

				return huddles;
			});

			Events.Huddle.AfterLoad.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddle.HuddleType != 0)
				{
					// Get the permits:
					huddle.Invites = await List(context, new Filter<HuddlePermittedUser>().Equals("HuddleId", huddle.Id));
				}

				return huddle;
			});
			
			Events.Huddle.AfterUpdate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddle.HuddleType != 0)
				{
					// Get the permits:
					huddle.Invites = await List(context, new Filter<HuddlePermittedUser>().Equals("HuddleId", huddle.Id));
				}

				return huddle;
			});
			
			Events.Huddle.AfterCreate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddle.HuddleType != 0)
				{
					// Get the permits:
					huddle.Invites = await List(context, new Filter<HuddlePermittedUser>().Equals("HuddleId", huddle.Id));
				}

				return huddle;
			});
			
			Events.Huddle.AfterList.AddEventListener(async (Context context, List<Huddle> huddles) =>
			{
				if (huddles == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddles.Count == 0)
				{
					return huddles;
				}

				// Get the permits:
				var allPermits = await List(context, new Filter<HuddlePermittedUser>().EqualsSet("HuddleId", huddles.Select(huddle => huddle.Id)));
				
				// Filter them through to the actual huddles they're for:
				if(allPermits != null)
				{
					var huddleMap = new Dictionary<int, Huddle>();
					
					foreach(var huddle in huddles)
					{
						if(huddle == null)
						{
							continue;
						}
						huddleMap[huddle.Id] = huddle;
					}
					
					foreach(var permit in allPermits)
					{
						if(permit == null)
						{
							continue;
						}
						
						if(huddleMap.TryGetValue(permit.HuddleId, out Huddle huddle))
						{
							
							if(huddle.Invites == null)
							{
								huddle.Invites = new List<HuddlePermittedUser>();
							}
							
							huddle.Invites.Add(permit);
						}
					}
				}
				
				return huddles;
			});
			
		}
	}
    
}
