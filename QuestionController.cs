using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.QuestionBoards;

namespace Api.Questions
{
    /// <summary>
    /// Handles question endpoints.
    /// </summary>

    [Route("v1/question")]
	public partial class QuestionController : AutoController<Question>
    {
    }
}