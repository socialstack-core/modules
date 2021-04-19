using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System;
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
		public ReactionService(ReactionTypeService reactionTypes, ReactionCountService reactionCounts) : base(Events.Reaction)
        {
			
			_reactionTypes = reactionTypes;
			
			var totalField = reactionCounts.GetChangeField("Total");

			Events.Reaction.BeforeCreate.AddEventListener(async (Context context, Reaction reaction) =>
			{
				if (reaction == null)
                {
					return null;
                }

				// Ensure it's unique:
				var existingReaction = await List(context, new Filter<Reaction>().Equals("UserId", context.UserId).And().Equals("ContentTypeId", reaction.ContentTypeId).And().Equals("ContentId", reaction.ContentId), DataOptions.IgnorePermissions);

				if (existingReaction != null && existingReaction.Count > 0)
				{
					throw new PublicException("Reaction already exists", "reaction_unique");
				}

				// Add to counter:
				var counts = await reactionCounts.List(context, new Filter<ReactionCount>().Equals("ContentTypeId", reaction.ContentTypeId).And().Equals("ContentId", reaction.ContentId));

				if (counts != null && counts.Count > 0)
				{
					var counter = counts[0];
					await reactionCounts.Update(context, counter, (Context ctx, ReactionCount ct) =>
					{
						ct.Total++;
						ct.MarkChanged(totalField);
					}, DataOptions.IgnorePermissions);
				}
				else
				{
					await reactionCounts.Create(context, new ReactionCount() {
						ContentTypeId = reaction.ContentTypeId,
						ContentId = reaction.ContentId,
						Total = 1,
						ReactionTypeId = reaction.ReactionTypeId,
						CreatedUtc = DateTime.UtcNow
					});
				}

				return reaction;

			});

		}
	}

}
