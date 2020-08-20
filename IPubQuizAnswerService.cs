using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizAnswers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPubQuizAnswerService
    {
		/// <summary>
		/// Delete a pubQuizAnswer by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a pubQuizAnswer by its ID.
		/// </summary>
		Task<PubQuizAnswer> Get(Context context, int id);

		/// <summary>
		/// Create a pubQuizAnswer.
		/// </summary>
		Task<PubQuizAnswer> Create(Context context, PubQuizAnswer e);

		/// <summary>
		/// Updates the database with the given pubQuizAnswer data. It must have an ID set.
		/// </summary>
		Task<PubQuizAnswer> Update(Context context, PubQuizAnswer e);

		/// <summary>
		/// List a filtered set of pubQuizAnswers.
		/// </summary>
		/// <returns></returns>
		Task<List<PubQuizAnswer>> List(Context context, Filter<PubQuizAnswer> filter);

	}
}
