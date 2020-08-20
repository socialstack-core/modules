using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizzes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPubQuizService
    {
		/// <summary>
		/// Delete a PubQuiz by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a PubQuiz by its ID.
		/// </summary>
		Task<PubQuiz> Get(Context context, int id);

		/// <summary>
		/// Create a PubQuiz.
		/// </summary>
		Task<PubQuiz> Create(Context context, PubQuiz e);

		/// <summary>
		/// Updates the database with the given PubQuiz data. It must have an ID set.
		/// </summary>
		Task<PubQuiz> Update(Context context, PubQuiz e);

		/// <summary>
		/// List a filtered set of pubQuizzes.
		/// </summary>
		/// <returns></returns>
		Task<List<PubQuiz>> List(Context context, Filter<PubQuiz> filter);

	}
}
