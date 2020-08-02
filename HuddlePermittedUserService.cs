using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddlePermittedUsers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddlePermittedUserService : AutoService<HuddlePermittedUser>, IHuddlePermittedUserService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddlePermittedUserService() : base(Events.HuddlePermittedUser)
        {
			// Example admin page install:
			// InstallAdminPages("HuddlePermittedUsers", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
