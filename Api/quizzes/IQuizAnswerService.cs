using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles creations of quiz answers
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IQuizAnswerService
	{
		/// <summary>
		/// Deletes a quiz answer by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int quizAnswerId);

		/// <summary>
		/// Gets a single quiz answer by its ID.
		/// </summary>
		Task<QuizAnswer> Get(Context context, int quizAnswerId);

		/// <summary>
		/// Creates a new quiz answer.
		/// </summary>
		Task<QuizAnswer> Create(Context context, QuizAnswer quizAnswer);

		/// <summary>
		/// Updates the given quiz answer.
		/// </summary>
		Task<QuizAnswer> Update(Context context, QuizAnswer quizAnswer);

		/// <summary>
		/// List a filtered set of quiz answer.
		/// </summary>
		/// <returns></returns>
		Task<List<QuizAnswer>> List(Context context, Filter<QuizAnswer> filter);
	}
}
