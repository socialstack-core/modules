using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.Linq;
using Api.Users;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;

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

				if (permit.InvitedContentId != 0)
				{
					permit.InvitedContent = await Content.Get(context, permit.InvitedContentTypeId, permit.InvitedContentId);
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
				
				if(permit.InvitedContentId != 0)
				{
					permit.InvitedContent = await Content.Get(context, permit.InvitedContentTypeId, permit.InvitedContentId);
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

				if (permit.InvitedContentId != 0)
				{
					permit.InvitedContent = await Content.Get(context, permit.InvitedContentTypeId, permit.InvitedContentId);
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

				await Content.ApplyMixed(
					context,
					huddles,
					src => {
						var invite = src as HuddlePermittedUser;
						return new ContentTypeAndId(invite.InvitedContentTypeId, invite.InvitedContentId);
					},
					(object src, object content) => {
						var invite = src as HuddlePermittedUser;
						invite.InvitedContent = content;
					}
				);

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

				if (huddle.HuddleType != 0 && huddle.Invites == null)
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

			var userContentType = ContentTypes.GetId(typeof(User));

			// Hook up a MultiSelect on the underlying fields:
			Events.Huddle.BeforeSettable.AddEventListener((Context rootContext, JsonField<Huddle> field) =>
			{
				if (field != null && field.Name == "Invites")
				{
					field.Module = "UI/MultiSelect";
					field.Data["contentType"] = "userprofile";
						
					// Defer the set after the ID is available:
					field.AfterId = true;

					// On set, convert provided IDs into tag objects.
					field.OnSetValueUnTyped.AddEventListener(async (Context ctx, object[] valueArgs) =>
					{
						if (valueArgs == null || valueArgs.Length < 2)
						{
							return null;
						}

						// The value should be an array of ints.
						var value = valueArgs[0];

						// The object we're setting to will have an ID now because of the above defer:
						var huddle = valueArgs[1] as Huddle;
						if (huddle == null)
						{
							return null;
						}

						if (!(value is JArray idArray))
						{
							return null;
						}

						var typeAndId = new List<ContentTypeAndId>();

						foreach (var token in idArray)
						{
							// Each token should be 

							if (token is JObject)
							{
								// {contentTypeId: x, id: y}
								var cTypeToken = token["contentTypeId"];
								var idToken = token["id"];

								if (cTypeToken != null && idToken != null)
								{
									var cType = cTypeToken.Value<int?>();
									var id = idToken.Value<int?>();

									if (id.HasValue && id > 0)
									{
										// If contentTypeId is not provided, user is assumed.
										typeAndId.Add(
											new ContentTypeAndId(
												cType.HasValue && cType > 0 ? cType.Value : userContentType,
												id.Value
											)
										);
									}
								}
							}
							else if (token is JValue)
							{
								// Convenience case for an array of IDs (user IDs).
								var id = token.Value<int?>();

								if (id.HasValue && id > 0)
								{
									typeAndId.Add(new ContentTypeAndId(userContentType, id.Value));
								}
							}

						}
							
						int revisionId = 0;
						if (huddle.RevisionId.HasValue)
						{
							revisionId = huddle.RevisionId.Value;
						}

						// Get all invite entries for this host object:
						var existingEntries = await List(
							ctx,
							new Filter<HuddlePermittedUser>().Equals("HuddleId", huddle.Id) //.And().Equals("RevisionId", revisionId)
						);

						// Identify ones being deleted, and ones being added, then update invite contents.
						// Note that ContentTypeAndId is a struct and does have a custom Equals/ HashCode etc.
						var existingLookup = new Dictionary<ContentTypeAndId, HuddlePermittedUser>();

						foreach (var existingEntry in existingEntries)
						{
							var cTypeAndId = new ContentTypeAndId(
								existingEntry.InvitedContentTypeId,
								existingEntry.InvitedContentId
							);
							existingLookup[cTypeAndId] = existingEntry;
						}

						var now = DateTime.UtcNow;

						var newSet = new Dictionary<ContentTypeAndId, bool>();

						foreach (var id in typeAndId)
						{
							newSet[id] = true;

							if (!existingLookup.ContainsKey(id))
							{
								// Add it:
								var newUser = new HuddlePermittedUser()
								{
									UserId = ctx.UserId,
									InvitedContentTypeId = id.ContentTypeId,
									InvitedContentId = id.ContentId,
									// An invited user becomes permitted when they accept.
									PermittedUserId = 0, // PermittedUserId = id.ContentTypeId == userContentType ? id.ContentId : 0,
									HuddleId = huddle.Id,
									RevisionId = revisionId,
									CreatedUtc = now,
									EditedUtc = now
								};

								existingLookup[id] = newUser;
								await Create(ctx, newUser);
							}
						}

						// Delete any being removed:
						foreach (var existingEntry in existingEntries)
						{
							// (Works because ContentTypeAndId is a struct).
							var cTypeAndId = new ContentTypeAndId(
								existingEntry.InvitedContentTypeId,
								existingEntry.InvitedContentId
							);

							if (!newSet.ContainsKey(cTypeAndId))
							{
								existingLookup.Remove(cTypeAndId);

								// Delete this row:
								await Delete(ctx, existingEntry.Id);
							}
						}
						
						if (typeAndId.Count == 0)
						{
							// Empty set to return.
							return null;
						}

						// Get the invite set:
						return existingLookup.Values.ToList();
					});

						
				}

				return Task.FromResult(field);
			});
			
		}
	}
    
}
