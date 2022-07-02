using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Questions;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Answers
{
    /// <summary>
    /// Handles answer endpoints.
    /// </summary>

    [Route("v1/answer")]
	public partial class AnswerController : AutoController<Answer>
    {
        private IQuestionService _questions;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AnswerController(
			IQuestionService questions
        )
        {
            _questions = questions;

			// Connect a create event:
			Events.Answer.BeforeCreate.AddEventListener(async (Context context, Answer answer) => {

				// Get the question so we can ensure the board ID is correct:
				var question = await _questions.Get(context, answer.QuestionId);

				if (question == null)
				{
					return null;
				}

				answer.QuestionBoardId = question.QuestionBoardId;
				return answer;
			});

		}
		
	}
}