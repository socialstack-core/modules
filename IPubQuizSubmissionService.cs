using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizSubmissions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPubQuizSubmissionService
    {
		/// <summary>
		/// Delete a pubQuizSubmission by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a pubQuizSubmission by its ID.
		/// </summary>
		Task<PubQuizSubmission> Get(Context context, int id);

		/// <summary>
		/// Create a pubQuizSubmission.
		/// </summary>
		Task<PubQuizSubmission> Create(Context context, PubQuizSubmission e);

		/// <summary>
		/// Updates the database with the given pubQuizSubmission data. It must have an ID set.
		/// </summary>
		Task<PubQuizSubmission> Update(Context context, PubQuizSubmission e);

		/// <summary>
		/// List a filtered set of pubQuizSubmissions.
		/// </summary>
		/// <returns></returns>
		Task<List<PubQuizSubmission>> List(Context context, Filter<PubQuizSubmission> filter);

	}
}
