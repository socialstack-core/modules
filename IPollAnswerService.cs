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
	public partial interface IPollAnswerService
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
		Task<PollAnswer> Get(Context context, int id);

		/// <summary>
		/// Create a poll.
		/// </summary>
		Task<PollAnswer> Create(Context context, PollAnswer e);

		/// <summary>
		/// Updates the database with the given poll data. It must have an ID set.
		/// </summary>
		Task<PollAnswer> Update(Context context, PollAnswer e);

		/// <summary>
		/// List a filtered set of polls.
		/// </summary>
		/// <returns></returns>
		Task<List<PollAnswer>> List(Context context, Filter<PollAnswer> filter);

	}
}
