using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System;

namespace Api.Rewards
{
	/// <summary>
	/// Handles rewards - usually given to users, but can go on any entity which implements IHaveRewards.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RewardService : AutoService<Reward>, IRewardService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RewardService() : base(Events.Reward)
        {
			
			// Create admin pages if they don't already exist:
			InstallAdminPages("Rewards", "fa:fa-award", new string[]{"id", "name"});
			
			// Because of IHaveRewards, Reward must be nestable:
			MakeNestable();
			
			// Define the IHaveTags handler:
			DefineIHaveArrayHandler<IHaveRewards, Reward, RewardContent>(
				"Rewards",
				"RewardId",
				(IHaveRewards content, List<Reward> results) =>
				{
					content.Rewards = results;
				}
			);
		}
		
	}
    
}
