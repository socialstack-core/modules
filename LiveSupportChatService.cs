using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.LiveSupportChats
{
	/// <summary>
	/// Handles liveSupportChats.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LiveSupportChatService : AutoService<LiveSupportChat>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LiveSupportChatService() : base(Events.LiveSupportChat)
        {
			// Example admin page install:
			// InstallAdminPages("LiveSupportChats", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
