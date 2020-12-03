using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Handles reactions - likes, upvotes etc - on content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ReactionService : AutoService<Reaction>
	{
		private ReactionTypeService _reactionTypes;

		private readonly Query<ReactionCount> listCountQuery;
		private readonly Query<ReactionCount> listCountByObjectQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionService(ReactionTypeService reactionTypes) : base(Events.Reaction)
        {
			
			_reactionTypes = reactionTypes;
			/*
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listCountQuery = Query.List<ReactionCount>();
			listCountByObjectQuery = Query.List<ReactionCount>();
			listCountByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);
			*/

			// Because of IHaveReactions, Reaction must be nestable:
			MakeNestable();

			// Load reactions on Load/List events next. First, find all events for types that implement IHaveReactions:
			var loadEvents = Events.FindByType(typeof(IHaveReactions), "Load", EventPlacement.After);

			// Define the IHaveReactions handler:
			DefineIHaveArrayHandler<IHaveReactions, ReactionCount>(
				(IHaveReactions content, List<ReactionCount> results) =>
                {
					content.Reactions = results;
                }
			);


			Events.Reaction.BeforeCreate.AddEventListener(async (Context context, Reaction reaction) =>
			{
				if (reaction == null)
                {
					return null;
                }

				if(context.UserId == null)
				{
					return null;
                }

				if (reaction.ReactionTypeId == null)
                {
					return null;
                }

				reaction.ReactionType = await _reactionTypes.Get(context, reaction.ReactionTypeId);

				// Now, list all reactions that have the same contentTypeId, contentId and UserId
				var reactions = await List(context, new Filter<Reaction>().Equals("UserId", context.UserId).And().Equals("ContentTypeId", reaction.ContentTypeId).And().Equals("ContentId", reaction.ContentId));

				// Let's now loop over each reaction
				foreach(var react in reactions)
                {
					if(react.ReactionType != null && react.ReactionType.GroupId == reaction.ReactionType.GroupId)
                    {
						await Delete(context, react);
                    }
                }

				return reaction;

			});

			Events.Reaction.AfterLoad.AddEventListener(async (Context context, Reaction reaction) =>
			{
				if (reaction == null)
				{
					return null;
				}
				reaction.ReactionType = await _reactionTypes.Get(context, reaction.ReactionTypeId);
				return reaction;
			});

			Events.Reaction.AfterList.AddEventListener(async (Context context, List<Reaction> reactions) =>
			{
				if (reactions == null || reactions.Count == 0)
				{
					return reactions;
				}
				// Collect all unique reactionType IDs:
				Dictionary<int, ReactionType> reactionTypes = new Dictionary<int, ReactionType>();
				foreach (var reaction in reactions)
				{
					if (reaction.ReactionTypeId == 0)
					{
						continue;
					}
					reactionTypes[reaction.ReactionTypeId] = null;
				}
				if (reactionTypes.Count == 0)
				{
					// Nothing to do - just return here:
					return reactions;
				}
				// Array of unique IDs:
				var reactionTypeIds = new object[reactionTypes.Count];
				var index = 0;
				foreach (var kvp in reactionTypes)
				{
					reactionTypeIds[index++] = kvp.Key;
				}
				// Get now:
				var reactionTypeFilter = new Filter<ReactionType>();
				reactionTypeFilter.EqualsSet("Id", reactionTypeIds);
				var reactionTypeData = await _reactionTypes.List(context, reactionTypeFilter);
				// Add the reactionType info to the lookup:
				foreach (var reactionType in reactionTypeData)
				{
					reactionTypes[reactionType.Id] = reactionType;
				}
				// Apply reactionTypes to reactions:
				foreach (var reaction in reactions)
				{
					if (reaction.ReactionTypeId == 0)
					{
						continue;
					}
					if (reactionTypes.TryGetValue(reaction.ReactionTypeId, out ReactionType rt))
					{
						reaction.ReactionType = rt;
					}
					else
					{
						reaction.ReactionType = null;
					}
				}
				return reactions;
			});

			/*

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveReactions and a DatabaseRow object:
					if (!(args[0] is IHaveReactions reactionableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Reactions are only available on DatabaseRow entities. " +
							"This type implements IHaveReactions but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(reactionableObject.GetType());

					// List the reaction counts now:
					var contentReactions = await _database.List(context, listCountByObjectQuery, null, contentTypeId, dbObject.Id);

					if (contentReactions == null || contentReactions.Count == 0)
					{
						// None found - just set an empty reaction array.
						reactionableObject.Reactions = new List<ReactionCount>();
					}
					else if (contentReactions.Count == 1)
					{
						// Use a higher speed prepared statement:
						var reactionType = await _reactionTypes.Get(context, contentReactions[0].ReactionTypeId);

						contentReactions[0].ReactionType = reactionType;

						reactionableObject.Reactions = contentReactions;
					}
					else
					{
						// It has multiple reaction types. Use a filtered list here.
						var filter = new Filter<ReactionType>();

						// These IDs will always be unique (but it doesn't matter if they're not).
						var ids = new object[contentReactions.Count];
						for (var i = 0; i < ids.Length; i++)
						{
							ids[i] = contentReactions[i].ReactionTypeId;
						}

						filter.EqualsSet("Id", ids);
						// Note - this list will often be really short (<5).
						// It represents the count of each type of reaction on a particular piece of content.
						var types = await _reactionTypes.List(context, filter);

						// Build a lookup if there's more than ~20 - otherwise a linear scan is faster:
						if (types.Count > 20)
						{

							var reactionLookup = new Dictionary<int, ReactionType>();

							foreach (var type in types)
							{
								reactionLookup[type.Id] = type;
							}

							foreach (var reaction in contentReactions)
							{
								reactionLookup.TryGetValue(reaction.ReactionTypeId, out ReactionType reactionType);
								reaction.ReactionType = reactionType;
							}
						}
						else
						{
							// Linear lookup is faster than building a dictionary. 
							// Can't loop more than 400 times in total (20^2).
							foreach (var reaction in contentReactions)
							{
								for (var i = 0; i < types.Count; i++)
								{
									if (types[i].Id == reaction.ReactionTypeId)
									{
										reaction.ReactionType = types[i];
										break;
									}
								}
							}
						}

						reactionableObject.Reactions = contentReactions;
					}

					return reactionableObject;
				});

			}

			// Next the List events:
			var listEvents = Events.FindByType(typeof(IHaveReactions), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveReactions implementors.
					if (!(args[0] is IList list))
					{
						// Can't handle this (or it was null anyway):
						return args[0];
					}

					// First we'll collect all their IDs so we can do a single bulk lookup.
					// ASSUMPTION: The list is not excessively long!
					// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
					// (applies to at least categories/ tags).

					var ids = new object[list.Count];
					var contentLookup = new Dictionary<int, IHaveReactions>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveReactions ihc))
						{
							throw new System.Exception("Reactions are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the tags to it shortly:
						contentLookup[entry.Id] = ihc;

						// Setup empty reaction count array:
						ihc.Reactions = new List<ReactionCount>();

						ids[i] = entry.Id;
					}

					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var filter = new Filter<ReactionCount>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids);

					// Get all the content reactions for these entities:
					var allContentReactions = await _database.List(context, listCountQuery, filter, contentTypeId);

					// Build fast reaction lookup:
					var reactionLookup = new Dictionary<int, ReactionType>();

					// Get the unique set of reaction IDs so we can collect those reactions. Shortly will reuse the same lookup dict.
					foreach (var contentReaction in allContentReactions)
					{
						reactionLookup[contentReaction.ReactionTypeId] = null;
					}

					if (reactionLookup.Count == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Array of unique tag IDs:
					var reactionIds = new object[reactionLookup.Count];
					var index = 0;

					foreach (var kvp in reactionLookup)
					{
						reactionIds[index++] = kvp.Key;
					}

					var reactionFilter = new Filter<ReactionType>();
					reactionFilter.EqualsSet("Id", reactionIds);
					var reactions = await _reactionTypes.List(context, reactionFilter);

					foreach (var reaction in reactions)
					{
						reactionLookup[reaction.Id] = reaction;
					}

					// For each content->reaction relation..
					foreach (var contentReaction in allContentReactions)
					{
						// Lookup content/ category:
						var content = contentLookup[contentReaction.ContentId];
						var reactionType = reactionLookup[contentReaction.ReactionTypeId];
						contentReaction.ReactionType = reactionType;

						// Add the reaction to the content:
						content.Reactions.Add(contentReaction);
					}

					return list;
				});

			}
			*/

		}
	}

}
