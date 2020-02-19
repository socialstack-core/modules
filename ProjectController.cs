using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Projects
{
    /// <summary>
    /// Handles project endpoints.
    /// </summary>

    [Route("v1/project")]
	[ApiController]
	public partial class ProjectController : ControllerBase
    {
        private IProjectService _projects;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ProjectController(
			IProjectService projects

		)
        {
			_projects = projects;
        }

		/// <summary>
		/// GET /v1/project/2/
		/// Returns the project data for a single project.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Project> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _projects.Get(context, id);
			return await Events.ProjectLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/project/2/
		/// Deletes an project
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Project> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _projects.Get(context, id);
			result = await Events.ProjectDelete.Dispatch(context, result, Response);

			if (result == null || !await _projects.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
			return result;
		}

		/// <summary>
		/// GET /v1/project/list
		/// Lists all projects available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Project>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/project/list
		/// Lists filtered projects available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Project>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Project>(filters);

			filter = await Events.ProjectList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _projects.List(context, filter);
			return new Set<Project>() { Results = results };
		}

		/// <summary>
		/// POST /v1/project/
		/// Creates a new project. Returns the ID.
		/// </summary>
		[HttpPost]
		[HttpPost]
		public async Task<Project> Create([FromBody] ProjectAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var prj = new Project
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, prj))
			{
				return null;
			}

			form = await Events.ProjectCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			prj = await _projects.Create(context, form.Result);

			if (prj == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return prj;
        }

		/// <summary>
		/// POST /v1/project/1/
		/// Updates an project with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Project> Update([FromRoute] int id, [FromBody] ProjectAutoForm form)
		{
			var context = Request.GetContext();

			var prj = await _projects.Get(context, id);
			
			if (!ModelState.Setup(form, prj)) {
				return null;
			}

			form = await Events.ProjectUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			prj = await _projects.Update(context, form.Result);

			if (prj == null)
			{
				Response.StatusCode = 500;
				return null;
			}

			return prj;
		}
		
    }

}
