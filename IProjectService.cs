using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Projects
{
	/// <summary>
	/// Handles projects - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IProjectService
    {
		/// <summary>
		/// Delete a project by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a project by its ID.
		/// </summary>
		Task<Project> Get(Context context, int id);

		/// <summary>
		/// Create a new project.
		/// </summary>
		Task<Project> Create(Context context, Project prj);

		/// <summary>
		/// Updates the database with the given project data. It must have an ID set.
		/// </summary>
		Task<Project> Update(Context context, Project prj);

		/// <summary>
		/// List a filtered set of projects.
		/// </summary>
		/// <returns></returns>
		Task<List<Project>> List(Context context, Filter<Project> filter);

	}
}
