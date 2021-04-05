using System;
using Api.Database;
using Api.Translate;
using Api.WebSockets;


namespace Api.Rewards
{
	
	/// <summary>
	/// Content tagged with a particular reward.
	/// </summary>
	public partial class RewardContent : MappingEntity, IAmLive
	{
		/// <summary>
		/// The ID of the reward.
		/// </summary>
		public uint RewardId;
		
		/// <summary>
		/// ID of the reward.
		/// </summary>
		public override uint TargetContentId
		{
			get{
				return RewardId;
			}
			set {
				RewardId = value;
			}
		}
	}
	
}