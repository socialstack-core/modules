using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Huddles
{
	/// <summary>
	/// Handles huddlePermittedUsers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IHuddlePermittedUserService
    {
		/// <summary>
		/// Delete a huddlePermittedUser by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a huddlePermittedUser by its ID.
		/// </summary>
		Task<HuddlePermittedUser> Get(Context context, int id);

		/// <summary>
		/// Create a huddlePermittedUser.
		/// </summary>
		Task<HuddlePermittedUser> Create(Context context, HuddlePermittedUser e);

		/// <summary>
		/// Updates the database with the given huddlePermittedUser data. It must have an ID set.
		/// </summary>
		Task<HuddlePermittedUser> Update(Context context, HuddlePermittedUser e);

		/// <summary>
		/// List a filtered set of huddlePermittedUsers.
		/// </summary>
		/// <returns></returns>
		Task<List<HuddlePermittedUser>> List(Context context, Filter<HuddlePermittedUser> filter);

	}
}
