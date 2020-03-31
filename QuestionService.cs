using Api.Database;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.Answers;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.QuestionBoards;

namespace Api.Questions
{
	/// <summary>
	/// Handles creations of questions - containers for answers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QuestionService : AutoService<Question>, IQuestionService
    {
        private IQuestionBoardService _questionBoards;
		
		private readonly Query<Answer> deleteAnswersQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuestionService(IDatabaseService database, IQuestionBoardService questionBoards) : base(Events.Question)
        {
            _database = database;
			_questionBoards = questionBoards;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteAnswersQuery = Query.Delete<Answer>();
			deleteAnswersQuery.Where().EqualsArg("QuestionId", 0);
        }
		
        /// <summary>
        /// Deletes a question by its ID.
		/// Includes deleting all answers and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Delete(Context context, int id)
        {
            // Delete the entry:
			await base.Delete(context, id);
			
			// Delete their replies:
			await _database.Run(context, deleteAnswersQuery, id);
			
			// Ok!
			return true;
        }

        /// <summary>
        /// Deletes a question by its ID.
		/// Optionally includes deleting all answers and uploaded content refs in there too.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id, bool deleteAnswers)
		{
			// Delete the entry:
			await base.Delete(context, id);

			if (deleteAnswers){
				// Delete their replies:
				await _database.Run(context, deleteAnswersQuery, id);
			}
			
			// Ok!
			return true;
        }

		/// <summary>
		/// Creates a new question.
		/// </summary>
		public override async Task<Question> Create(Context context, Question question)
		{
			// Get the board to obtain the default page ID:
			var board = await _questionBoards.Get(context, question.QuestionBoardId);

			if (board == null)
			{
				// board doesn't exist!
				return null;
			}

			if (question.PageId == 0)
			{
				// Default page ID applied now:
				question.PageId = board.QuestionPageId;
			}
			
			return await base.Create(context, question);
		}
	}

}
