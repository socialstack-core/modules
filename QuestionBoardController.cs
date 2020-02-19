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
	public partial class QuestionBoardController : AutoController<QuestionBoard, QuestionBoardAutoForm>
    {
    }
}