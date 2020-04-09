using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Followers
{
	/// <summary>
	/// Handles users who follow other users.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IFollowerService
	{
		/// <summary>
		/// Deletes a follower by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single follower by its ID.
		/// </summary>
		Task<Follower> Get(Context context, int id);

		/// <summary>
		/// Creates a new follower.
		/// </summary>
		Task<Follower> Create(Context context, Follower follower);

		/// <summary>
		/// Updates the given follower.
		/// </summary>
		Task<Follower> Update(Context context, Follower follower);

		/// <summary>
		/// List a filtered set of followers.
		/// </summary>
		/// <returns></returns>
		Task<List<Follower>> List(Context context, Filter<Follower> filter);
	}
}
