using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles creations of quiz questions - containers for answers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IQuizQuestionService
	{
		/// <summary>
		/// Deletes a quiz question by its ID.
		/// Optionally includes deleting all answers and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int quizQuestionId);

		/// <summary>
		/// Gets a single quiz question by its ID.
		/// </summary>
		Task<QuizQuestion> Get(Context context, int quizQuestionId);

		/// <summary>
		/// Creates a new quiz question.
		/// </summary>
		Task<QuizQuestion> Create(Context context, QuizQuestion quizQuestion);

		/// <summary>
		/// Updates the given quiz question.
		/// </summary>
		Task<QuizQuestion> Update(Context context, QuizQuestion quizQuestion);

		/// <summary>
		/// List a filtered set of quiz questions.
		/// </summary>
		/// <returns></returns>
		Task<List<QuizQuestion>> List(Context context, Filter<QuizQuestion> filter);
	}
}
