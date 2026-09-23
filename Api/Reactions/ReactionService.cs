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
		private readonly ReactionCountService _reactionCounts;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionService(ReactionTypeService reactionTypes, ReactionCountService reactionCounts) : base(Events.Reaction)
        {
			_reactionTypes = reactionTypes;
			_reactionCounts = reactionCounts;
			
			Events.Reaction.BeforeCreate.AddEventListener(async (Context context, Reaction reaction) =>
			{
				if (reaction == null)
                {
					return null;
                }

				// Ensure it's unique:
				var existingReaction = await Where(
					"UserId=? and ContentType=? and ContentId=?",
					DataOptions.IgnorePermissions
				).Bind(context.UserId).Bind(reaction.ContentType).Bind(reaction.ContentId).Any(context);
				
				if (existingReaction)
				{
					throw new PublicException("Reaction already exists", "reaction_unique");
				}

				await ChangeReactionOnContentForType(context, reaction, false);

				return reaction;

			});

			// Remove the reaction from the reaction counts on the content when it's deleted.
			Events.Reaction.AfterDelete.AddEventListener(async (Context context, Reaction reaction) =>
			{
				if (reaction == null)
				{
					return null;
				}

				await ChangeReactionOnContentForType(context, reaction, true);

				return reaction;
			});

		}

		/// <summary>
		/// Locates the service for the content type a reaction is on, then binds and invokes ChangeReactionOnContent.
		/// </summary>
		private async ValueTask ChangeReactionOnContentForType(Context context, Reaction reaction, bool isDelete)
		{
			var svc = Services.Get(reaction.ContentType + "Service");

			if (svc == null)
			{
				if (isDelete)
				{
					return;
				}

				throw new PublicException("Content not found", "content_not_found");
			}

			var method = typeof(ReactionService).GetMethod(nameof(ChangeReactionOnContent));

			if (method == null)
			{
				return;
			}

			var genericMethod = method.MakeGenericMethod(svc.ServicedType, svc.IdType);

			await (ValueTask)genericMethod.Invoke(this, new object[] { context, svc, reaction, isDelete });
		}

		/// <summary>
		/// Inc/ dec the reaction based on if it is a delete or not.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="context"></param>
		/// <param name="service"></param>
		/// <param name="reaction"></param>
		/// <param name="isDelete"></param>
		/// <returns></returns>
		public async ValueTask ChangeReactionOnContent<T, ID>(Context context, AutoService<T, ID> service, Reaction reaction, bool isDelete)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Get the content this reaction is for.
			var content = await service.Get(context, service.ConvertId(reaction.ContentId), DataOptions.IgnorePermissions);

			if (content == null)
			{
				if (isDelete)
				{
					// When deleting a reaction the content it was on may have already been deleted.
					return;
				}

				// The content doesn't exist, so we can't change the reaction count for it.
				throw new PublicException("Content not found", "content_not_found");
			}

			if (isDelete)
			{
				await RemoveReactionFromContent(context, service, content, reaction);
			}
			else
			{
				await AddReactionToContent(context, service, content, reaction);
			}
		}

		/// <summary>
		/// Adds to the reaction counts on a given piece of content.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <returns></returns>
		public async ValueTask AddReactionToContent<T, ID>(Context context, AutoService<T, ID> service, T content, Reaction reaction)
			where T: Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Load the reaction counts for this content from the mappings.
			var counts = await _reactionCounts.ListBySource(context, content, "reactions", DataOptions.IgnorePermissions);

			// Check each one for a ReactionCount of this reaction.ReactionType.
			ReactionCount existing = null;

			foreach (var count in counts)
			{
				if (count.ReactionTypeId == reaction.ReactionTypeId)
				{
					existing = count;
					break;
				}
			}

			if (existing == null)
			{
				// If it doesn't exist, create a new ReactionCount object for it, and add it to the content.Mappings.
				var newCount = await _reactionCounts.Create(context, new ReactionCount()
				{
					ReactionTypeId = reaction.ReactionTypeId,
					Total = 1
				}, DataOptions.IgnorePermissions);

				await service.Update(context, content, (Context c, T updated, T original) =>
				{
					updated.Mappings.Add("reactions", newCount.Id);
				}, DataOptions.IgnorePermissions);
			}
			else
			{
				// Otherwise, increase the ReactionCount count by 1
				await _reactionCounts.Update(context, existing, (Context c, ReactionCount updated, ReactionCount original) =>
				{
					updated.Total = original.Total + 1;
				}, DataOptions.IgnorePermissions);
			}
		}

		/// <summary>
		/// Removes from the reaction counts on a given piece of content.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <returns></returns>
		public async ValueTask RemoveReactionFromContent<T, ID>(Context context, AutoService<T, ID> service, T content, Reaction reaction)
			where T: Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Load the reaction counts for this content from the mappings.
			var counts = await _reactionCounts.ListBySource(context, content, "reactions", DataOptions.IgnorePermissions);

			// Check each one for a ReactionCount of this reaction.ReactionType.
			ReactionCount existing = null;

			foreach (var count in counts)
			{
				if (count.ReactionTypeId == reaction.ReactionTypeId)
				{
					existing = count;
					break;
				}
			}

			if (existing == null)
			{
				// Nothing to remove.
				return;
			}

			// Decrease the ReactionCount count by 1
			var updatedCount = await _reactionCounts.Update(context, existing, (Context c, ReactionCount updated, ReactionCount original) =>
			{
				updated.Total = original.Total - 1;
			}, DataOptions.IgnorePermissions);

			// If the total is now zero or below, remove it from the content.Mappings and delete the ReactionCount.
			if (updatedCount == null || updatedCount.Total <= 0)
			{
				await service.Update(context, content, (Context c, T updated, T original) =>
				{
					updated.Mappings.Remove("reactions", existing.Id);
				}, DataOptions.IgnorePermissions);

				await _reactionCounts.Delete(context, existing, DataOptions.IgnorePermissions);
			}
		}
	}

}
