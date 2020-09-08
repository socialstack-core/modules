using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Matchmaking
{
	/// <summary>
	/// Handles matches.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IMatchService
    {
		/// <summary>
		/// Delete a match by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a match by its ID.
		/// </summary>
		Task<Match> Get(Context context, int id);

		/// <summary>
		/// Create a match.
		/// </summary>
		Task<Match> Create(Context context, Match e);

		/// <summary>
		/// Updates the database with the given match data. It must have an ID set.
		/// </summary>
		Task<Match> Update(Context context, Match e);
		
		/// <summary>
		/// Generates a join link for the given match.
		/// </summary>
		Task<string> Join(Context context, Match e);
		
		/// <summary>
		/// Matchmakes using the given matchmaker.
		/// </summary>
		Task<Match> Matchmake(Context context, int matchmakerId, int teamSize);
		
		/// <summary>
		/// List a filtered set of matches.
		/// </summary>
		/// <returns></returns>
		Task<List<Match>> List(Context context, Filter<Match> filter);

	}
}
