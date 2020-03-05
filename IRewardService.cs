using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Rewards
{
	/// <summary>
	/// Handles rewards - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IRewardService
    {
		/// <summary>
		/// Delete a reward by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a reward by its ID.
		/// </summary>
		Task<Reward> Get(Context context, int id);

		/// <summary>
		/// Create a new reward.
		/// </summary>
		Task<Reward> Create(Context context, Reward reward);

		/// <summary>
		/// Updates the database with the given reward data. It must have an ID set.
		/// </summary>
		Task<Reward> Update(Context context, Reward reward);

		/// <summary>
		/// List a filtered set of rewards.
		/// </summary>
		/// <returns></returns>
		Task<List<Reward>> List(Context context, Filter<Reward> filter);

	}
}
