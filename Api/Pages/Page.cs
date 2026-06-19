using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Pages
{

	/// <summary>
	/// A page. Pages are accessed via associated permalink(s).
	/// </summary>
	[HasVirtualField("pageGroup", typeof(PageGroup), "PageGroupId")]
	public partial class Page : VersionedContent<uint>
	{
		/// <summary>
		/// The default title for this page.
		/// </summary>
		public Localized<string> Title;

		/// <summary>
		/// A key of the form e.g. "admin_user_list" which is used to keep track of 
		/// generated pages, enabling the URL to be edited without causing a page to regenerate.
		/// A page taking on the role of primary content for a given type has a key set to e.g. "primary:user".
		/// If it is the primary page for a specific piece of content, then it is e.g. "primary:product:42".
		/// Primary keys on the admin panel are prefixed with "admin_".
		/// </summary>
		public string Key;

		/// <summary>
		/// The latest RevisionId at the point of page install.
		/// </summary>
		[JsonIgnore]
		public uint? LastInstallRevisionId;
		
		/// <summary>
		/// The latest build time that an install check happened.
		/// </summary>
		[JsonIgnore]
		public DateTime? LastInstallBuildTimeUtc;

		/// <summary>
		/// The type of content that this page will attempt to load using information from the URL. 
		/// If the Key contains "primary:x" then this will be inferred from the key.
		/// </summary>
		public string PrimaryContentType;
		
		/// <summary>
		/// The pages content (as canvas JSON).
		/// </summary>
		[Data("tab", "design")]
		[Data("groups", "*")]
		[DatabaseField(Length = 9000000)]
		public Localized<JsonString> BodyJson;

		/// <summary>
		/// The default description for this page.
		/// </summary>
		public Localized<string> Description;

		/// <summary>
		/// Allow this page from being indexed by search crawlers. 
		/// It is opt-in to avoid any automatic indexing of private pages.
		/// </summary>
		[Data("hint", "Allow search crawlers and the sitemap to index this page")]
		public bool CanIndex;

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

		/// <summary>
		/// The includes string to use for primary content. * will include everything at 1 level deep.
		/// </summary>
		public string PrimaryContentIncludes;
		
		/// <summary>
		/// The ID of the page group.
		/// </summary>
		public uint PageGroupId;
		
		/// <summary>
		/// A temporarily held URL value which is used during page creation to create a new permalink.
		/// </summary>
		[Module("Admin/Page/UrlEditor")]
		public string Url { get; set; }
	}
	
}