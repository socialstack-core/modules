using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Forums
{
    /// <summary>
    /// Used when creating or updating a forum
    /// </summary>
    public partial class ForumAutoForm : AutoForm<Forum>
	{
		/// <summary>
		/// The primary ID of the page that this forum appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The page ID that threads will appear on.
		/// </summary>
		public int ThreadPageId;

		/// <summary>
		/// The name of the forum in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// A short description of this forum.
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
