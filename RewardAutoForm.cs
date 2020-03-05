using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Rewards
{
    /// <summary>
    /// Used when creating or updating a reward
    /// </summary>
    public partial class RewardAutoForm : AutoForm<Reward>
    {
		/// <summary>
		/// The name of the new reward in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// Description of this reward.
		/// </summary>
		public string Description;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;
	}
}
