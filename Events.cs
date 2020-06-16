using Api.Rewards;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for reward content.
		/// </summary>
		public static EventGroup<RewardContent> RewardContent;
		
		/// <summary>
		/// Set of events for a Reward.
		/// </summary>
		public static EventGroup<Reward> Reward;
	}
}