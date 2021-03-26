using System;
using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.Rewards
{
	
	/// <summary>
	/// A reward.
	/// These can be added to any IHaveRewards type.
	/// </summary>
	public partial class Reward : VersionedContent<int>
	{
		/// <summary>
		/// The name of the reward in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Name;
		
		/// <summary>
		/// Used to identify a reward without knowing the name.
		/// E.g. "10_upvotes"
		/// </summary>
		public string Key;
		
		/// <summary>
		/// Description of this reward.
		/// </summary>
		[Localized]
		public string Description;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}
	
}