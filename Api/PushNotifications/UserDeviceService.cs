using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PushNotifications
{
	/// <summary>
	/// Handles userDevices.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserDeviceService : AutoService<UserDevice>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserDeviceService() : base(Events.UserDevice)
        {
			// Example admin page install:
			// InstallAdminPages("UserDevices", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
