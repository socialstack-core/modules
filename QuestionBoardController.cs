using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.QuestionBoards
{
	/// <summary>
	/// Handles question board endpoints.
	/// </summary>

	[Route("v1/questionboard")]
	[ApiController]
	public partial class QuestionBoardController : ControllerBase
    {
        private IQuestionBoardService _questionBoards;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public QuestionBoardController(
			IQuestionBoardService questionBoards
        )
        {
            _questionBoards = questionBoards;
        }

		/// <summary>
		/// GET /v1/questionboard/2/
		/// Returns the question board data for a single question board.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<QuestionBoard> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _questionBoards.Get(context, id);
			return await Events.QuestionBoardLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/questionboard/2/
		/// Deletes a question board
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _questionBoards.Get(context, id);
			result = await Events.QuestionBoardDelete.Dispatch(context, result, Response);

			if (result == null || !await _questionBoards.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/questionboard/list
		/// Lists all question boards available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<QuestionBoard>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/questionboard/list
		/// Lists filtered question boards available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<QuestionBoard>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<QuestionBoard>(filters);

			filter = await Events.QuestionBoardList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _questionBoards.List(context, filter);
			return new Set<QuestionBoard>() { Results = results };
		}
		
		/// <summary>
		/// POST /v1/questionboard/
		/// Creates a new question board. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<QuestionBoard> Create([FromBody] QuestionBoardAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var questionBoard = new QuestionBoard
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, questionBoard))
			{
				return null;
			}

			form = await Events.QuestionBoardCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			questionBoard = await _questionBoards.Create(context, form.Result);

			if (questionBoard == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return questionBoard;
        }
		
		/// <summary>
		/// POST /v1/questionboard/1/
		/// Updates a question board with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<QuestionBoard> Update([FromRoute] int id, [FromBody] QuestionBoardAutoForm form)
		{
			var context = Request.GetContext();

			var questionBoard = await _questionBoards.Get(context, id);
			
			if (!ModelState.Setup(form, questionBoard)) {
				return null;
			}

			form = await Events.QuestionBoardUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			questionBoard = await _questionBoards.Update(context, form.Result);

			if (questionBoard == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return questionBoard;
		}
		
    }

}
