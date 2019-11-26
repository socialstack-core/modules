using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Handles reaction types - creation of custom likes/ dislikes etc.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IReactionTypeService
    {

		/// <summary>
		/// Deletes a reaction type by its ID.
		/// </summary>
		Task<bool> Delete(Context context, int reactionId);

		/// <summary>
		/// Gets a single reaction by its ID.
		/// </summary>
		Task<ReactionType> Get(Context context, int reactionId);

		/// <summary>
		/// Creates a new reaction type.
		/// </summary>
		Task<ReactionType> Create(Context context, ReactionType reaction);

		/// <summary>
		/// Updates the given reaction type.
		/// </summary>
		Task<ReactionType> Update(Context context, ReactionType reaction);

		/// <summary>
		/// List a filtered set of reaction types.
		/// </summary>
		/// <returns></returns>
		Task<List<ReactionType>> List(Context context, Filter<ReactionType> filter);

	}
}
