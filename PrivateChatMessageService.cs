using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PrivateChatMessages
{
	/// <summary>
	/// Handles privateChatMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PrivateChatMessageService : AutoService<PrivateChatMessage>, IPrivateChatMessageService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PrivateChatMessageService() : base(Events.PrivateChatMessage)
        {
			// Example admin page install:
			// InstallAdminPages("PrivateChatMessages", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
