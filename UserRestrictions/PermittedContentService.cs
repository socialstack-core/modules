using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Permissions
{
	/// <summary>
	/// Handles permittedContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PermittedContentService : AutoService<PermittedContent>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PermittedContentService() : base(Events.PermittedContent)
        {
			// Example admin page install:
			// InstallAdminPages("PermittedContents", "fa:fa-rocket", new string[] { "id", "name" });
			
			// Caching these has a general performance improvement given many filters use them.
			Cache();
		}
	}
    
}
