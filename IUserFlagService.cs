using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.UserFlags
{
	/// <summary>
	/// Handles userFlags.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IUserFlagService
    {
		/// <summary>
		/// Delete an userFlag by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get an userFlag by its ID.
		/// </summary>
		Task<UserFlag> Get(Context context, int id);

		/// <summary>
		/// Create an userFlag.
		/// </summary>
		Task<UserFlag> Create(Context context, UserFlag e);

		/// <summary>
		/// Updates the database with the given userFlag data. It must have an ID set.
		/// </summary>
		Task<UserFlag> Update(Context context, UserFlag e);

		/// <summary>
		/// List a filtered set of userFlags.
		/// </summary>
		/// <returns></returns>
		Task<List<UserFlag>> List(Context context, Filter<UserFlag> filter);

	}
}
