using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Rewards
{
	/// <summary>
	/// Handles rewardContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IRewardContentService
    {
		/// <summary>
		/// Delete a rewardContent by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a rewardContent by its ID.
		/// </summary>
		Task<RewardContent> Get(Context context, int id);

		/// <summary>
		/// Create a rewardContent.
		/// </summary>
		Task<RewardContent> Create(Context context, RewardContent e);

		/// <summary>
		/// Updates the database with the given rewardContent data. It must have an ID set.
		/// </summary>
		Task<RewardContent> Update(Context context, RewardContent e);

		/// <summary>
		/// List a filtered set of rewardContents.
		/// </summary>
		/// <returns></returns>
		Task<List<RewardContent>> List(Context context, Filter<RewardContent> filter);

	}
}
