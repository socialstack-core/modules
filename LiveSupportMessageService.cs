using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.LiveSupportChats
{
	/// <summary>
	/// Handles liveSupportMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LiveSupportMessageService : AutoService<LiveSupportMessage>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LiveSupportMessageService() : base(Events.LiveSupportMessage)
        {
		}
	}
    
}
