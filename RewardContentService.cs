using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Rewards
{
	/// <summary>
	/// Handles rewardContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RewardContentService : AutoService<RewardContent>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RewardContentService() : base(Events.RewardContent)
        {
		}
	}
    
}
