using System;
using Api.Database;
using Api.Translate;


namespace Api.Rewards
{
	
	/// <summary>
	/// Content tagged with a particular reward.
	/// </summary>
	public partial class RewardContent : DatabaseRow
	{
		/// <summary>
		/// The ID of the reward.
		/// </summary>
		public int RewardId;
		/// <summary>
		/// The type ID of the rewardd content. See also: Api.Database.ContentTypes
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The ID of the rewardd content.
		/// </summary>
		public int ContentId;
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;
	}
	
}