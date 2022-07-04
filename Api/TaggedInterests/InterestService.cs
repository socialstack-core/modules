using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Interests
{
	/// <summary>
	/// Handles interests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class InterestService : AutoService<Interest>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public InterestService() : base(Events.Interest)
        {
			// Example admin page install:
			// InstallAdminPages("Interests", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
