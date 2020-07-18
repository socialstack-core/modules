using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.FrequentlyAskedQuestions
{
	/// <summary>
	/// Handles frequentlyAskedQuestions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IFrequentlyAskedQuestionService
    {
		/// <summary>
		/// Delete a frequentlyAskedQuestion by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a frequentlyAskedQuestion by its ID.
		/// </summary>
		Task<FrequentlyAskedQuestion> Get(Context context, int id);

		/// <summary>
		/// Create a frequentlyAskedQuestion.
		/// </summary>
		Task<FrequentlyAskedQuestion> Create(Context context, FrequentlyAskedQuestion e);

		/// <summary>
		/// Updates the database with the given frequentlyAskedQuestion data. It must have an ID set.
		/// </summary>
		Task<FrequentlyAskedQuestion> Update(Context context, FrequentlyAskedQuestion e);

		/// <summary>
		/// List a filtered set of frequentlyAskedQuestions.
		/// </summary>
		/// <returns></returns>
		Task<List<FrequentlyAskedQuestion>> List(Context context, Filter<FrequentlyAskedQuestion> filter);

	}
}
