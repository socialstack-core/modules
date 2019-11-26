using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Tags
{
    /// <summary>
    /// Used when creating or updating a tag
    /// </summary>
    public partial class TagAutoForm : AutoForm<Tag>
    {
		/// <summary>
		/// The name of the new tag in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// Description of this tag.
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

		/// <summary>
		/// The Hex color we want the tag to be.
		/// </summary>
		public string HexColor;
	}
}
