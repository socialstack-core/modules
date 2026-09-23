using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forums - containers for forum threads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumService : AutoService<Forum>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumService() : base(Events.Forum)
        {
			InstallAdminPages("Forums", "fa:fa-th-list", new string[] { "id", "name" });
		}
	}
    
}
