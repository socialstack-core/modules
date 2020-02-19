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
	public partial class QuestionBoardService : AutoService<QuestionBoard>, IQuestionBoardService
    {
		private readonly Query<Question> deleteQuestionsQuery;
		private readonly Query<Answer> deleteAnswersQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuestionBoardService(IDatabaseService database) : base(Events.QuestionBoard)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuestionsQuery = Query.Delete<Question>();
			deleteQuestionsQuery.Where().EqualsArg("QuestionBoardId", 0);

			deleteAnswersQuery = Query.Delete<Answer>();
			deleteAnswersQuery.Where().EqualsArg("QuestionBoardId", 0);
		}
		
        /// <summary>
        /// Deletes a question board by its ID.
		/// Optionally includes deleting all question content in there too.
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Delete(Context context, int id)
        {
            return await Delete(context, id, true);
        }

        /// <summary>
        /// Deletes a question board by its ID.
		/// Optionally includes deleting all question content in there too.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteQuestions)
        {
            // Delete the questionBoard entry:
			await base.Delete(context, id);
			
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
	}
}