using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ActiveLogins
{
	/// <summary>
	/// Handles activeLogins.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IActiveLoginService
    {
		/// <summary>
		/// Delete an activeLogin by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get an activeLogin by its ID.
		/// </summary>
		Task<ActiveLogin> Get(Context context, int id);

		/// <summary>
		/// Create an activeLogin.
		/// </summary>
		Task<ActiveLogin> Create(Context context, ActiveLogin e);

		/// <summary>
		/// Updates the database with the given activeLogin data. It must have an ID set.
		/// </summary>
		Task<ActiveLogin> Update(Context context, ActiveLogin e);

		/// <summary>
		/// List a filtered set of activeLogins.
		/// </summary>
		/// <returns></returns>
		Task<List<ActiveLogin>> List(Context context, Filter<ActiveLogin> filter);

	}
}
