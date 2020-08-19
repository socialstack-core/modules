using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ActiveLogins
{
	/// <summary>
	/// Handles ActiveLoginHistory.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IActiveLoginHistoryService
    {
		/// <summary>
		/// Delete an ActiveLoginHistory by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get an ActiveLoginHistory by its ID.
		/// </summary>
		Task<ActiveLoginHistory> Get(Context context, int id);

		/// <summary>
		/// Create an ActiveLoginHistory.
		/// </summary>
		Task<ActiveLoginHistory> Create(Context context, ActiveLoginHistory e);

		/// <summary>
		/// Updates the database with the given ActiveLoginHistory data. It must have an ID set.
		/// </summary>
		Task<ActiveLoginHistory> Update(Context context, ActiveLoginHistory e);

		/// <summary>
		/// List a filtered set of activeLogins.
		/// </summary>
		/// <returns></returns>
		Task<List<ActiveLoginHistory>> List(Context context, Filter<ActiveLoginHistory> filter);

	}
}
