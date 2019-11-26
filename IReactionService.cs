using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Handles reactions - likes, upvotes etc - on content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IReactionService
    {

		/// <summary>
		/// Deletes a reaction by its ID.
		/// </summary>
		Task<bool> Delete(Context context, int reactionId);

		/// <summary>
		/// Gets a single reaction by its ID.
		/// </summary>
		Task<Reaction> Get(Context context, int reactionId);

		/// <summary>
		/// Creates a new reaction.
		/// </summary>
		Task<Reaction> Create(Context context, Reaction reaction);

		/// <summary>
		/// Updates the given reaction.
		/// </summary>
		Task<Reaction> Update(Context context, Reaction reaction);

		/// <summary>
		/// List a filtered set of reactions.
		/// </summary>
		/// <returns></returns>
		Task<List<Reaction>> List(Context context, Filter<Reaction> filter);

	}
}
