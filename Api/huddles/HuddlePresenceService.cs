using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddlePresence.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddlePresenceService : AutoService<HuddlePresence>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddlePresenceService() : base(Events.HuddlePresence)
        {
			// Example admin page install:
			// InstallAdminPages("HuddlePresence", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
