using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Galleries
{
    /// <summary>
    /// Used when creating or updating a gallery
    /// </summary>
    public partial class GalleryAutoForm : AutoForm<Gallery>
    {
		/// <summary>
		/// The primary ID of the page that this gallery appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The page ID that entries will appear on.
		/// </summary>
		public int EntryPageId;

		/// <summary>
		/// The name of the gallery in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// A description of this gallery.
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
