using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Articles
{
	
	/// <summary>
	/// An article, typically used in e.g. help guides or knowledge bases.
	/// </summary>
	public partial class Article : VersionedContent<int>
	{
		/// <summary>
		/// The name of the article in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Name;

		/// <summary>
		/// The primary ID of the page that this article appears on.
		/// </summary>
		public int PageId;
		
		/// <summary>
		/// The content of this article.
		/// </summary>
		[Localized]
		public string BodyJson;

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

        /// <summary>
        /// The description of the article
        /// </summary>
        [DatabaseField(Length = 500)]
        public string Description;     
	}
	
}