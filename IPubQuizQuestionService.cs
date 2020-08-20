using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizQuestions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPubQuizQuestionService
    {
		/// <summary>
		/// Delete a pubQuizQuestion by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a pubQuizQuestion by its ID.
		/// </summary>
		Task<PubQuizQuestion> Get(Context context, int id);

		/// <summary>
		/// Create a pubQuizQuestion.
		/// </summary>
		Task<PubQuizQuestion> Create(Context context, PubQuizQuestion e);

		/// <summary>
		/// Updates the database with the given pubQuizQuestion data. It must have an ID set.
		/// </summary>
		Task<PubQuizQuestion> Update(Context context, PubQuizQuestion e);

		/// <summary>
		/// List a filtered set of pubQuizQuestions.
		/// </summary>
		/// <returns></returns>
		Task<List<PubQuizQuestion>> List(Context context, Filter<PubQuizQuestion> filter);

	}
}
