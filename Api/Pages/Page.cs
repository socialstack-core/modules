using System;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;

namespace Api.Pages
{
	
	/// <summary>
	/// A page.
	/// </summary>
	public partial class Page : VersionedContent<uint>
	{
		/// <summary>
		/// The URL for this page.
		/// </summary>
		public string Url;
		
		/// <summary>
		/// The default title for this page.
		/// </summary>
		[Localized]
		public string Title;
		
		/// <summary>
		/// The pages content (as canvas JSON).
		/// </summary>
		[Localized]
		[Data("groups", "*")]
		[DatabaseField(Length = 9000000)]
		public string BodyJson;

		/// <summary>
		/// The default description for this page.
		/// </summary>
		[Localized]
		public string Description;

		/// <summary>
		/// Prevent this page from being indexed by search crawlers.
		/// </summary>
		[Data("hint", "Prevent search crawlers from indexing this page")]
		public bool NoIndex;

		/// <summary>
		/// Prevent links on this page from being followed by search crawlers.
		/// </summary>
		[Data("hint", "Prevent search crawlers from following links on this page")]
		public bool NoFollow;

		/// <summary>
		/// A disambiguation mechanism when the permission system returns multiple pages.
		/// Typically happens on the homepage.
		/// </summary>
		public bool PreferIfLoggedIn;
	}
	
}