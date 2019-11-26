using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Categories
{
    /// <summary>
    /// Used when creating or updating a category
    /// </summary>
    public partial class CategoryAutoForm : AutoForm<Category>
	{
		/// <summary>
		/// The name of the category in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// Description of this category.
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
