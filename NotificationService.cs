using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Notifications
{
	/// <summary>
	/// Handles notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NotificationService : AutoService<Notification>, INotificationService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NotificationService() : base(Events.Notification)
        {
			// Example admin page install:
			// InstallAdminPages("Notifications", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
