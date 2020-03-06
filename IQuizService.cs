using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles creations of quizzes
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IQuizService
	{
		/// <summary>
		/// Deletes a quiz by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int quizId);

		/// <summary>
		/// Gets a single quiz by its ID.
		/// </summary>
		Task<Quiz> Get(Context context, int quizId);

		/// <summary>
		/// Creates a new quiz.
		/// </summary>
		Task<Quiz> Create(Context context, Quiz quiz);

		/// <summary>
		/// Updates the given quiz.
		/// </summary>
		Task<Quiz> Update(Context context, Quiz quiz);

		/// <summary>
		/// List a filtered set of quiz.
		/// </summary>
		/// <returns></returns>
		Task<List<Quiz>> List(Context context, Filter<Quiz> filter);
	}
}
