using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Polls
{
	/// <summary>
	/// Handles polls.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PollService : AutoService<Poll>, IPollService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollService() : base(Events.Poll)
        {
			// Example admin page install:
			InstallAdminPages("Polls", "fa:fa-poll", new string[] { "id", "title" });
		}
	}
    
}
