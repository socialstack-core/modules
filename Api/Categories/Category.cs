using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;

namespace Api.Categories
{
	
	/// <summary>
	/// A category.
	/// These are the primary taxonomy mechanism; any site content can be grouped up in multiple categories.
	/// </summary>
	[ListAs("Categories", Tab = "tags_categories")]
	public partial class Category : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the category in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public Localized<string> Name;
		
		/// <summary>
		/// Description of this category.
		/// </summary>
		public Localized<string> Description;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 300)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
	    [DatabaseField(Length = 300)]
		public string IconRef;
	}
	
}
