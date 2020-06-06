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
	public partial class PollAnswerService : AutoService<PollAnswer>, IPollAnswerService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollAnswerService() : base(Events.PollAnswer)
        {
			// Example admin page install:
			// InstallAdminPages("Polls", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
