using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Projects
{
	/// <summary>
	/// Handles projects.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProjectService : AutoService<Project>, IProjectService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProjectService() : base(Events.Project)
        {
			InstallAdminPages("Projects", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
