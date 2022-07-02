using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PublishGroups
{
	/// <summary>
	/// Handles publishGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PublishGroupService : AutoService<PublishGroup>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PublishGroupService() : base(Events.PublishGroup)
        {
			InstallAdminPages("Publish Groups", "fa:fa-book-open", new string[] { "id", "name" });
		}
	}
    
}
