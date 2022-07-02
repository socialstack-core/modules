using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Answers
{
	/// <summary>
	/// Handles answers (on questions).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IAnswerService
	{
		/// <summary>
		/// Deletes an answer by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single answer by its ID.
		/// </summary>
		Task<Answer> Get(Context context, int id);

		/// <summary>
		/// Creates a new answer.
		/// </summary>
		Task<Answer> Create(Context context, Answer answer);

		/// <summary>
		/// Updates the given answer.
		/// </summary>
		Task<Answer> Update(Context context, Answer answer);

		/// <summary>
		/// List a filtered set of answers.
		/// </summary>
		/// <returns></returns>
		Task<List<Answer>> List(Context context, Filter<Answer> filter);
	}
}
