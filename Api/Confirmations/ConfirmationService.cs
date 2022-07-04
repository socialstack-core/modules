using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Confirmations
{
	/// <summary>
	/// Handles confirmations.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ConfirmationService : AutoService<Confirmation>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ConfirmationService() : base(Events.Confirmation)
        {
			// Example admin page install:
			// InstallAdminPages("Confirmations", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
