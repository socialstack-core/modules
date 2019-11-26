using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Blogs
{
    /// <summary>
    /// Used when creating or updating a blog
    /// </summary>
    public partial class BlogAutoForm : AutoForm<Blog>
    {
		/// <summary>
		/// The name of the blog in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// The primary ID of the page that this blog appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The page ID that posts from this blog will appear on.
		/// </summary>
		public int PostPageId;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;

		/// <summary>
		/// The description of the blog that will appear on the blog list.
		/// </summary>
		public string Description;
	}
}
