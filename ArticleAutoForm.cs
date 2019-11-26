using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Articles
{
    /// <summary>
    /// Used when creating or updating an article
    /// </summary>
    public partial class ArticleAutoForm : AutoForm<Article>
    {
		/// <summary>
		/// The name of the article in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// The primary ID of the page that this article appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The content of this article.
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;

		/// <summary>
		/// The description of the article
		/// </summary>
		public string Description;
	}
}
