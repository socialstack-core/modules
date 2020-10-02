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
    public partial class HuddlePermittedUserService : AutoService<HuddlePermittedUser>
    {
		
		private int userContentType;
        private HuddleService _huddleService;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public HuddlePermittedUserService(UserService users, HuddleService huddleService) : base(Events.HuddlePermittedUser)
        {

            _huddleService = huddleService;
            userContentType = ContentTypes.GetId(typeof(User));

            Events.HuddlePermittedUser.BeforeCreate.AddEventListener(async (Context context, HuddlePermittedUser permit) =>
            {
                if (permit == null)
                {
                    // Due to the way how event chains work, the primary object can be null.
                    // Safely ignore this.
                    return null;
                }

                var huddle = await _huddleService.Get(context, permit.HuddleId);
                if (huddle == null)
                {
                    return null;
                }
				
                // Check that the invited content is accessible:
                if (permit.InvitedContentId != 0)
                {
                    permit.InvitedContent = await Content.Get(context, permit.InvitedContentTypeId, permit.InvitedContentId, true);
                }
				else
				{
					permit.InvitedContent = null;
				}
                
                // make sure the user is allowed to invite this entity to the huddle 
                if (context.UserId == huddle.CreatorUser.Id ||
                    (huddle.HuddleType == 1 && context.UserId == permit.InvitedContentId && permit.InvitedContentTypeId == ContentTypes.GetId("User")))
                {
                    return permit;
                }

                // people cannot create invites for someone else etc 
                return null;
            });


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
				else
				{
					permit.InvitedContent = null;
				}

                // Get the permitted user profile:
                if (permit.PermittedUserId != 0)
                {
                    permit.PermittedUser = await users.GetProfile(context, permit.PermittedUserId);
                }
				else
				{
					permit.PermittedUser = null;
				}

                return permit;
            });

            Events.HuddlePermittedUser.BeforeUpdate.AddEventListener(async (Context context, HuddlePermittedUser permit) =>
            {
                if (permit == null)
                {
                    // Due to the way how event chains work, the primary object can be null.
                    // Safely ignore this.
                    return null;
                }

                if (permit.InvitedContentId != 0)
                {
                    permit.InvitedContent = await Content.Get(context, permit.InvitedContentTypeId, permit.InvitedContentId, true);
                }
				else
				{
					permit.InvitedContent = null;
				}

                // Get the permitted user profile:
                if (permit.PermittedUserId != 0)
                {
                    permit.PermittedUser = await users.GetProfile(context, permit.PermittedUserId);
                }
				else
				{
					permit.PermittedUser = null;
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
				else
				{
					permit.PermittedUser = null;
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
                    src =>
                    {
                        var invite = src as HuddlePermittedUser;
                        return new ContentTypeAndId(invite.InvitedContentTypeId, invite.InvitedContentId);
                    },
                    (object src, object content) =>
                    {
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
						else
						{
							huddle.PermittedUser = null;
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
				else
				{
					huddle.Invites = null;
				}

                return huddle;
            }, 1);

            Events.Huddle.AfterUpdate.AddEventListener(async (Context context, Huddle huddle) =>
            {
                if (huddle == null)
                {
                    // Due to the way how event chains work, the primary object can be null.
                    // Safely ignore this.
                    return null;
                }
                
                // (Happens in a separate handler so others can inject before this).

                // If any have been accepted, they must be unaccepted IF the date changed since they accepted.
                // Importantly the invite gets adjusted to being direct to the permitted 
                // user, instead of whatever was invited originally.
                if (huddle.Invites != null)
                {
                    foreach (var invite in huddle.Invites)
                    {
                        if ( // It's accepted:
                            invite.PermittedUserId != 0 && !invite.Rejected &&
                            // And it's not "this" user:
                            invite.PermittedUserId != context.UserId && 
                            // And the time changed since it was accepted:
                            (invite.AcceptedStartUtc != huddle.StartTimeUtc || invite.AcceptedEndUtc != huddle.EstimatedEndTimeUtc))
                        {
                            // Someone (else) accepted but the time changed. We'll soft-unaccept the invite so they can check their schedule.
                            // Make sure invited content is now "them" if needed:
                            invite.InvitedContentTypeId = userContentType;
							invite.InvitedContentId = invite.PermittedUserId;
                            invite.PermittedUserId = 0;
                            invite.PermittedUser = null;

                            await Update(context, invite);
                        }

                    }
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
				else
				{
					huddle.Invites = null;
				}

                return huddle;
            });

            Events.Huddle.BeforeDelete.AddEventListener(async (Context context, Huddle huddle) =>
            {
                if (huddle == null)
                {
                    return huddle;
                }

                // Get the associated invites
                var invites = await List(context, new Filter<HuddlePermittedUser>().Equals("HuddleId", huddle.Id));

                // For each one, remove the permitted users
                foreach (var entry in invites)
                {
                    await Delete(context, entry.Id);
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
                if (allPermits != null)
                {
                    var huddleMap = new Dictionary<int, Huddle>();

                    foreach (var huddle in huddles)
                    {
                        if (huddle == null)
                        {
                            continue;
                        }
						huddle.Invites = null;
                        huddleMap[huddle.Id] = huddle;
                    }

                    foreach (var permit in allPermits)
                    {
                        if (permit == null)
                        {
                            continue;
                        }

                        if (huddleMap.TryGetValue(permit.HuddleId, out Huddle huddle))
                        {

                            if (huddle.Invites == null)
                            {
                                huddle.Invites = new List<HuddlePermittedUser>();
                            }

                            huddle.Invites.Add(permit);
                        }
                    }
                }

                return huddles;
            });

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
                    field.OnSetValue.AddEventListener(async (Context ctx, object value, Huddle huddle, JToken srcToken) =>
                    {
                        // The object we're setting to will have an ID now because of the above defer:
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
						
						// MUST handle "me" first to avoid creating invites if the user themselves can't actually schedule the meeting.
						foreach (var id in typeAndId)
                        {
							newSet[id] = true;
							
							if (existingLookup.ContainsKey(id) || !ctx.HasContent(id.ContentTypeId, id.ContentId))
							{
								continue;
							}
							
							// Add it.
							var newUser = await CreateAccepted(ctx, huddle, ctx.UserId, id.ContentTypeId, id.ContentId);
							
							existingLookup[id] = newUser;
						}
						
                        foreach (var id in typeAndId)
                        {
                            if (existingLookup.ContainsKey(id) || ctx.HasContent(id.ContentTypeId, id.ContentId))
							{
								continue;
							}
						
							// Add it.
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
							
							newUser = await Create(ctx, newUser);
							existingLookup[id] = newUser;
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

                return new ValueTask<JsonField<Huddle>>(field);
            });

        }
		
		/// <summary>
		/// Creates a pre-accepted permit.
		/// </summary>
		public Task<HuddlePermittedUser> CreateAccepted(Context context, Huddle huddle, int userId)
		{
			return CreateAccepted(context, huddle, userId, userContentType, userId);
		}
		
		/// <summary>
		/// Creates a pre-accepted permit.
		/// </summary>
		public async Task<HuddlePermittedUser> CreateAccepted(Context context, Huddle huddle, int userId, int contentTypeId, int contentId)
		{
			int revisionId = 0;
			if (huddle.RevisionId.HasValue)
			{
				revisionId = huddle.RevisionId.Value;
			}
			
			var now = DateTime.UtcNow;
			
			var newUser = new HuddlePermittedUser()
			{
				UserId = userId,
				InvitedContentTypeId = contentTypeId,
				InvitedContentId = contentId,
				// An invited user becomes permitted when they accept.
				PermittedUserId = 0, // PermittedUserId = id.ContentTypeId == userContentType ? id.ContentId : 0,
				HuddleId = huddle.Id,
				RevisionId = revisionId,
				CreatedUtc = now,
				EditedUtc = now
			};
			
			// E.g. "this" user is the invited one. Must immediately accept the invite (this makes it e.g. get added to schedules).
			newUser.PermittedUserId = userId;
			newUser.AcceptedStartUtc = huddle.StartTimeUtc;
			newUser.AcceptedEndUtc = huddle.EstimatedEndTimeUtc;
			newUser.Creator = userId == huddle.UserId;
			newUser = await Events.HuddlePermittedUser.BeforeAccept.Dispatch(context, newUser);
			
			newUser = await Create(context, newUser);
			
			if (newUser != null)
			{
				newUser = await Events.HuddlePermittedUser.AfterAccept.Dispatch(context, newUser);
			}
			
			return newUser;
		}
		
        /// <summary>
        /// Rejects or cancels a request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invite"></param>
        /// <returns></returns>
        public async Task<HuddlePermittedUser> RejectOrCancel(Context context, HuddlePermittedUser invite)
        {
            if (invite == null)
            {
                return null;
            }

            if (!invite.Rejected)
            {
                invite = await Events.HuddlePermittedUser.BeforeCancel.Dispatch(context, invite);

                if (invite == null)
                {
                    return null;
                }

                // Mark as rejected:
                invite.Rejected = true;

                // Clear the user ID:
                invite.PermittedUserId = 0;

                invite = await Update(context, invite);

                if (invite == null)
                {
                    return null;
                }

                invite = await Events.HuddlePermittedUser.AfterCancel.Dispatch(context, invite);
            }

            return invite;
        }

        /// <summary>
        /// Accepts a request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invite"></param>
        /// <param name="userId"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public async Task<HuddlePermittedUser> Accept(Context context, HuddlePermittedUser invite, int userId, bool force)
        {
            if (invite == null)
            {
                return null;
            }

            if (force)
            {
                if (invite.PermittedUserId != 0)
                {
                    // Clear the prior user by cancelling it:

                    invite = await Events.HuddlePermittedUser.BeforeCancel.Dispatch(context, invite);

                    if (invite == null)
                    {
                        return null;
                    }

                    // Clear the user ID:
                    invite.PermittedUserId = 0;

                    invite = await Events.HuddlePermittedUser.AfterCancel.Dispatch(context, invite);

                    if (invite == null)
                    {
                        return null;
                    }
                }
            }
            else if (invite.PermittedUserId != 0)
            {
				// It's already accepted.
                throw new PublicException("This invite has already been accepted.", "meeting_accepted");
            }

            var huddle = await _huddleService.Get(context, invite.HuddleId);

            if (huddle == null)
            {
                // The huddle doesn't exist.
                throw new PublicException("This meeting no longer exists.", "meeting_deleted");
            }

            invite.PermittedUserId = context.UserId;
            invite.AcceptedStartUtc = huddle.StartTimeUtc;
            invite.AcceptedEndUtc = huddle.EstimatedEndTimeUtc;
            invite = await Events.HuddlePermittedUser.BeforeAccept.Dispatch(context, invite);

            if (invite == null)
            {
                return null;
            }

            // Update the invite:
            invite = await Update(context, invite);

            if (invite == null)
            {
                return null;
            }

            invite = await Events.HuddlePermittedUser.AfterAccept.Dispatch(context, invite);
            return invite;
        }
    }

}
