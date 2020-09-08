using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Matchmaking
{
	/// <summary>
	/// Handles matchmakers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IMatchmakerService
    {
		/// <summary>
		/// Delete a matchmaker by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a matchmaker by its ID.
		/// </summary>
		Task<Matchmaker> Get(Context context, int id);

		/// <summary>
		/// Create a matchmaker.
		/// </summary>
		Task<Matchmaker> Create(Context context, Matchmaker e);

		/// <summary>
		/// Updates the database with the given matchmaker data. It must have an ID set.
		/// </summary>
		Task<Matchmaker> Update(Context context, Matchmaker e);

		/// <summary>
		/// List a filtered set of matchmakers.
		/// </summary>
		/// <returns></returns>
		Task<List<Matchmaker>> List(Context context, Filter<Matchmaker> filter);

	}
}
