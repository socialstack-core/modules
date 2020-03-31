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
        }

		/// <summary>
		/// POST /v1/answer/
		/// Creates a new answer. Returns the ID.
		/// </summary>
		[HttpPost]
		public override async Task<Answer> Create([FromBody] AnswerAutoForm form)
		{
			var context = Request.GetContext();

			// Get the question so we can grab the board ID:
			var question = await _questions.Get(context, form.QuestionId);

			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var answer = new Answer
			{
				UserId = context.UserId,
				QuestionBoardId = question.QuestionBoardId
			};
			
			if (!ModelState.Setup(form, answer))
			{
				return null;
			}

			form = await Events.Answer.Create.Dispatch(context, form, Response) as AnswerAutoForm;

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			answer = await _service.Create(context, form.Result);

			if (answer == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return answer;
        }
	}
}