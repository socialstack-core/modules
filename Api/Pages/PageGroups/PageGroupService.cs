using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Pages
{
	/// <summary>
	/// Handles pageGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PageGroupService : AutoService<PageGroup>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PageGroupService() : base(Events.PageGroup)
        {
			InstallAdminPages("Page Groups", "fa:fa-book", new string[] { "id", "name" });
		}
	}
    
}
