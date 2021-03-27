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
		private readonly ReactionTypeService _reactionTypes;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionService(ReactionTypeService reactionTypes) : base(Events.Reaction)
        {
			
			_reactionTypes = reactionTypes;
			
			// Because of IHaveReactions, Reaction must be nestable:
			MakeNestable();

			// Define the IHaveReactions handler:
			DefineIHaveArrayHandler(
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

				reaction.ReactionType = await _reactionTypes.Get(context, reaction.ReactionTypeId, DataOptions.IgnorePermissions);

				// Now, list all reactions that have the same contentTypeId, contentId and UserId
				var reactions = await List(context, new Filter<Reaction>().Equals("UserId", context.UserId).And().Equals("ContentTypeId", reaction.ContentTypeId).And().Equals("ContentId", reaction.ContentId), DataOptions.IgnorePermissions);

				// Let's now loop over each reaction
				foreach(var react in reactions)
                {
					if(react.ReactionType != null && react.ReactionType.GroupId == reaction.ReactionType.GroupId)
                    {
						await Delete(context, react, DataOptions.IgnorePermissions);
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
				reaction.ReactionType = await _reactionTypes.Get(context, reaction.ReactionTypeId, DataOptions.IgnorePermissions);
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
				var reactionTypeData = await _reactionTypes.List(context, reactionTypeFilter, DataOptions.IgnorePermissions);
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

		}
	}

}
