using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;

namespace Api.Components
{
	/// <summary>
	/// Handles componentGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ComponentGroupService : AutoService<ComponentGroup>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ComponentGroupService() : base(Events.ComponentGroup)
        {
			// Example admin page install:
			InstallAdminPages("Component Groups", "fa:fa-folder", ["id", "name"], null, "content_management");
		}
	}
    
}
