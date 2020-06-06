using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Polls
{
	/// <summary>
	/// Handles polls.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPollService
    {
		/// <summary>
		/// Delete a poll by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a poll by its ID.
		/// </summary>
		Task<Poll> Get(Context context, int id);

		/// <summary>
		/// Create a poll.
		/// </summary>
		Task<Poll> Create(Context context, Poll e);

		/// <summary>
		/// Updates the database with the given poll data. It must have an ID set.
		/// </summary>
		Task<Poll> Update(Context context, Poll e);

		/// <summary>
		/// List a filtered set of polls.
		/// </summary>
		/// <returns></returns>
		Task<List<Poll>> List(Context context, Filter<Poll> filter);

	}
}
