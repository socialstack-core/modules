using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Matchmaking
{
	/// <summary>
	/// Handles matchServers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IMatchServerService
    {
		/// <summary>
		/// Delete a matchServer by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a matchServer by its ID.
		/// </summary>
		Task<MatchServer> Get(Context context, int id);

		/// <summary>
		/// Create a matchServer.
		/// </summary>
		Task<MatchServer> Create(Context context, MatchServer e);

		/// <summary>
		/// Updates the database with the given matchServer data. It must have an ID set.
		/// </summary>
		Task<MatchServer> Update(Context context, MatchServer e);

		/// <summary>
		/// Allocates a match server for the given matchmaker, primarily considering region.
		/// </summary>
		MatchServer Allocate(Context context, Matchmaker matchmaker);

		/// <summary>
		/// List a filtered set of matchServers.
		/// </summary>
		/// <returns></returns>
		Task<List<MatchServer>> List(Context context, Filter<MatchServer> filter);

	}
}
