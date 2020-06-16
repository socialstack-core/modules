using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System;

namespace Api.Rewards
{
	/// <summary>
	/// Handles rewards - usually given to users, but can go on any entity which implements IHaveRewards.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RewardService : AutoService<Reward>, IRewardService
    {
		private readonly Query<RewardContent> listByObjectQuery;
		private readonly Query<RewardContent> listContentQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RewardService(IRewardContentService _rewardContents) : base(Events.Reward)
        {
			
			// Create admin pages if they don't already exist:
			InstallAdminPages("Rewards", "fa:fa-award", new string[]{"id", "name"});
			
			// Because of IHaveRewards, Reward must be nestable:
			MakeNestable();

			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listContentQuery = Query.List<RewardContent>();
			listByObjectQuery = Query.List<RewardContent>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);

			// Load rewards on Load/List events next. First, find all events for types that implement IHaveRewards:
			var loadEvents = Events.FindByType(typeof(IHaveRewards), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveRewards and a DatabaseRow object:
					if (!(args[0] is IHaveRewards rewardableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Rewards are only available on DatabaseRow entities. " +
							"This type implements IHaveRewards but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(rewardableObject.GetType());

					// List the rewards now:
					var contentRewards = await _database.List(context, listByObjectQuery, null, contentTypeId, dbObject.Id);

					if (contentRewards == null || contentRewards.Count == 0)
					{
						// None found - just set an empty reward array.
						rewardableObject.Rewards = new List<Reward>();
					}
					else if (contentRewards.Count == 1)
					{
						// Use a higher speed prepared statement:
						rewardableObject.Rewards = new List<Reward>(1)
						{
							await Get(context, contentRewards[0].RewardId)
						};
					}
					else
					{
						// It has multiple rewards. Use a filtered list here.
						var filter = new Filter<Reward>();

						var ids = new object[contentRewards.Count];
						for (var i = 0; i < ids.Length; i++)
						{
							ids[i] = contentRewards[i].RewardId;
						}

						filter.EqualsSet("Id", ids);
						rewardableObject.Rewards = await List(context, filter);
					}

					return rewardableObject;
				});

			}

			// Hook up a MultiSelect on the underlying fields:
			var listBeforeSettable = Events.FindByType(typeof(IHaveRewards), "Settable", EventPlacement.Before);

			foreach (var listEvent in listBeforeSettable)
			{
				listEvent.AddEventListener((Context context, object[] args) =>
				{
					var field = args[0] as JsonField;

					if (field != null && field.Name == "Rewards")
					{
						field.Module = "Admin/MultiSelect";
						field.Data["contentType"] = "reward";

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
							if (!(valueArgs[1] is DatabaseRow targetObject))
							{
								return null;
							}

							if (!(value is JArray idArray))
							{
								return null;
							}

							var ids = new List<int>();

							foreach (var token in idArray)
							{
								// id is..
								var id = token.Value<int?>();

								if (id.HasValue && id > 0)
								{
									ids.Add(id.Value);
								}
							}

							if (ids.Count == 0)
							{
								// Do nothing
								return null;
							}

							var contentTypeId = ContentTypes.GetId(targetObject.GetType());

							// Get all reward content entries for this host object:
							var existingEntries = await _rewardContents.List(
								ctx,
								new Filter<RewardContent>().Equals("ContentId", targetObject.Id).And().Equals("ContentTypeId", contentTypeId)
							);

							// Identify ones being deleted, and ones being added, then update tag contents.
							Dictionary<int, RewardContent> existingLookup = new Dictionary<int, RewardContent>();

							foreach (var existingEntry in existingEntries)
							{
								existingLookup[existingEntry.RewardId] = existingEntry;
							}

							var now = DateTime.UtcNow;

							Dictionary<int, bool> newSet = new Dictionary<int, bool>();

							foreach (var id in ids)
							{
								newSet[id] = true;

								if (!existingLookup.ContainsKey(id))
								{
									// Add it:
									await _rewardContents.Create(ctx, new RewardContent()
									{
										ContentId = targetObject.Id,
										ContentTypeId = contentTypeId,
										RewardId = id,
										CreatedUtc = now
									});
								}
							}

							// Delete any being removed:
							foreach (var existingEntry in existingEntries)
							{
								if (!newSet.ContainsKey(existingEntry.RewardId))
								{
									// Delete this row:
									await _rewardContents.Delete(ctx, existingEntry.Id);
								}
							}

							// Get the rewards:
							return await List(ctx, new Filter<Reward>().EqualsSet("Id", ids));
						});


					}

					return args[0];
				});
			}

			// Next the List events:
			var listEvents = Events.FindByType(typeof(IHaveRewards), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveRewards implementors.
					if (!(args[0] is IList list))
					{
						// Can't handle this (or it was null anyway):
						return args[0];
					}

					// First we'll collect all their IDs so we can do a single bulk lookup.
					// ASSUMPTION: The list is not excessively long!
					// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
					// (applies to at least categories/ tags/ rewards).

					var ids = new object[list.Count];
					var contentLookup = new Dictionary<int, IHaveRewards>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveRewards ihc))
						{
							throw new System.Exception("Rewards are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the rewards to it shortly:
						contentLookup[entry.Id] = ihc;

						// Setup empty reward array:
						ihc.Rewards = new List<Reward>();

						ids[i] = entry.Id;
					}

					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var filter = new Filter<RewardContent>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids);

					// Get all the content rewards for these entities:
					var allcontentRewards = await _database.List(context, listContentQuery, filter, contentTypeId);

					// Build fast reward lookup:
					var rewardLookup = new Dictionary<int, Reward>();

					// Get the unique set of reward IDs so we can collect those categories. Shortly will reuse the same lookup dict.
					foreach (var contentReward in allcontentRewards)
					{
						rewardLookup[contentReward.RewardId] = null;
					}

					if (rewardLookup.Count == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Array of unique reward IDs:
					var rewardIds = new object[rewardLookup.Count];
					var index = 0;

					foreach (var kvp in rewardLookup)
					{
						rewardIds[index++] = kvp.Key;
					}

					var rewardFilter = new Filter<Reward>();
					rewardFilter.EqualsSet("Id", rewardIds);
					var categories = await List(context, rewardFilter);

					foreach (var category in categories)
					{
						rewardLookup[category.Id] = category;
					}

					// For each content->reward relation..
					foreach (var contentCategory in allcontentRewards)
					{
						// Lookup content/ category:
						var content = contentLookup[contentCategory.ContentId];
						var category = rewardLookup[contentCategory.RewardId];

						// Add the reward to the content:
						content.Rewards.Add(category);
					}

					return list;
				});

			}

			// We'll handle where field "Rewards" ourselves:
			Filter.DeclareCustomWhereField("Rewards");

			// Next we'll add a handler for "Rewards" filters on List endpoints.
			// These are the *List events (but unlike above, we're using the "NotSpecified" placement):
			var listOnEvents = Events.FindByType(typeof(IHaveRewards), "List", EventPlacement.NotSpecified);

			foreach (var listEvent in listOnEvents)
			{
				listEvent.AddEventListener((Context context, object[] args) =>
				{
					// These always have:
					// args[0] is a filter object
					// args[1] is for the Response

					var filter = args[0] as Filter;

					if (filter == null || filter.FromRequest == null)
					{
						// We can't handle this - return the first arg just in case it was something else:
						return args[0];
					}

					var where = filter.FromRequest["where"] as JObject;

					if (where == null)
					{
						// No where clause
						return args[0];
					}

					// If the filter contained a "Rewards" key then we'll add a join restriction:
					var rewardSet = where["Rewards"] as JArray;

					if (rewardSet != null)
					{
						// We've got rewards that we need to filter by. For now this set must be integer reward IDs.
						// We want to join the reward content table 
						// on WhateverTypeTheFilterIsUsing.Id = RewardContent.Id AND ContentTypeId = TheIDOfThatFilterType
						// AND rewardId IN(rewardSet)

						// Need an object[] first:
						var idArray = new object[rewardSet.Count];

						for (var i = 0; i < idArray.Length; i++)
						{
							var rewardId = rewardSet[i].Value<int>();
							idArray[i] = rewardId;
						}

						filter.Join<RewardContent>("Id", "ContentId")
							.And().Equals("ContentTypeId", ContentTypes.GetId(filter.DefaultType))
							.And().EqualsSet("RewardId", idArray);

					}

					return args[0];
				});
			}

		}
		
	}
    
}
