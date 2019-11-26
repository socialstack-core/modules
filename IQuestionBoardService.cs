using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.QuestionBoards
{
	/// <summary>
	/// Handles creations of question boards - containers for questions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IQuestionBoardService
	{
        /// <summary>
        /// Deletes a board by its ID.
		/// Optionally includes deleting all replies, threads and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
		Task<bool> Delete(Context context, int boardId, bool deleteQuestions = true);

		/// <summary>
		/// Gets a single board by its ID.
		/// </summary>
		Task<QuestionBoard> Get(Context context, int boardId);

		/// <summary>
		/// Creates a new board.
		/// </summary>
		Task<QuestionBoard> Create(Context context, QuestionBoard board);

		/// <summary>
		/// Updates the given board.
		/// </summary>
		Task<QuestionBoard> Update(Context context, QuestionBoard board);

		/// <summary>
		/// List a filtered set of boards.
		/// </summary>
		/// <returns></returns>
		Task<List<QuestionBoard>> List(Context context, Filter<QuestionBoard> filter);

	}
}
