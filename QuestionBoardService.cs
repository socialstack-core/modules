using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Answers;
using Api.Questions;
using Api.Eventing;
using Api.Contexts;

namespace Api.QuestionBoards
{
	/// <summary>
	/// Handles creations of questionBoards - containers for questionBoard threads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QuestionBoardService : IQuestionBoardService
    {
        private IDatabaseService _database;
		
		private readonly Query<QuestionBoard> deleteQuestionBoardQuery;
		private readonly Query<Question> deleteQuestionsQuery;
		private readonly Query<Answer> deleteAnswersQuery;
		private readonly Query<QuestionBoard> createQuery;
		private readonly Query<QuestionBoard> selectQuery;
		private readonly Query<QuestionBoard> listQuery;
		private readonly Query<QuestionBoard> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuestionBoardService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuestionBoardQuery = Query.Delete<QuestionBoard>();

			deleteQuestionsQuery = Query.Delete<Question>();
			deleteQuestionsQuery.Where().EqualsArg("QuestionBoardId", 0);

			deleteAnswersQuery = Query.Delete<Answer>();
			deleteAnswersQuery.Where().EqualsArg("QuestionBoardId", 0);
			
			createQuery = Query.Insert<QuestionBoard>();
			updateQuery = Query.Update<QuestionBoard>();
			selectQuery = Query.Select<QuestionBoard>();
			listQuery = Query.List<QuestionBoard>();
		}
		
        /// <summary>
        /// Deletes a question board by its ID.
		/// Optionally includes deleting all question content in there too.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteQuestions = true)
        {
            // Delete the questionBoard entry:
			await _database.Run(deleteQuestionBoardQuery, id);
			
			if(deleteQuestions)
			{
				// Delete questions:
				await _database.Run(deleteQuestionsQuery, id);
				
				// Delete their answers:
				await _database.Run(deleteAnswersQuery, id);
			}
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List a filtered set of question boards.
		/// </summary>
		/// <returns></returns>
		public async Task<List<QuestionBoard>> List(Context context, Filter<QuestionBoard> filter)
		{
			filter = await Events.QuestionBoardBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.QuestionBoardAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Gets a single question board by its ID.
		/// </summary>
		public async Task<QuestionBoard> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new question board.
		/// </summary>
		public async Task<QuestionBoard> Create(Context context, QuestionBoard questionBoard)
		{
			questionBoard = await Events.QuestionBoardBeforeCreate.Dispatch(context, questionBoard);

			// Note: The Id field is automatically updated by Run here.
			if (questionBoard == null || !await _database.Run(createQuery, questionBoard))
			{
				return null;
			}

			questionBoard = await Events.QuestionBoardAfterCreate.Dispatch(context, questionBoard);
			return questionBoard;
		}

		/// <summary>
		/// Updates the given question board.
		/// </summary>
		public async Task<QuestionBoard> Update(Context context, QuestionBoard questionBoard)
		{
			questionBoard = await Events.QuestionBoardBeforeUpdate.Dispatch(context, questionBoard);

			if (questionBoard == null || !await _database.Run(updateQuery, questionBoard, questionBoard.Id))
			{
				return null;
			}

			questionBoard = await Events.QuestionBoardAfterUpdate.Dispatch(context, questionBoard);
			return questionBoard;
		}

	}

}
