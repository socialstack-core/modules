using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Questions
{
	/// <summary>
	/// Handles creations of questions - containers for answers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IQuestionService
	{
		/// <summary>
		/// Deletes a question by its ID.
		/// Optionally includes deleting all answers and uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int questionId, bool deleteAnswers = true);

		/// <summary>
		/// Gets a single question by its ID.
		/// </summary>
		Task<Question> Get(Context context, int questionId);

		/// <summary>
		/// Creates a new question.
		/// </summary>
		Task<Question> Create(Context context, Question question);

		/// <summary>
		/// Updates the given question.
		/// </summary>
		Task<Question> Update(Context context, Question question);

		/// <summary>
		/// List a filtered set of questions.
		/// </summary>
		/// <returns></returns>
		Task<List<Question>> List(Context context, Filter<Question> filter);
	}
}
