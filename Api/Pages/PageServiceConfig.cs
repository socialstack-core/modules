using Api.AutoForms;
using Api.Configuration;

namespace Api.Pages
{
	
	/// <summary>
	/// Config for PageService
	/// </summary>
	public class PageServiceConfig : Config
	{
		/// <summary>
		/// True if default pages should be installed when they don't exist. This is only checked at startup.
		/// </summary>
		public bool InstallDefaultPages { get; set; } = true;


		/// <summary>
		/// True if debug information should be logged when installing default pages. This is only checked at startup.
		/// SHOULD BE OFF UNLESS SPECIFICALLY DEBUGGING PAGE INSTALLS
		/// </summary>
		public bool DebugPageInstalls { get; set; } = false;

		/// <summary>
		/// The default value when adding a new page for indexed by search crawlers. 
		/// </summary>
		[Frontend]
		[Data("hint", "Allow search crawlers and the sitemap to index this page")]
        public bool DefaultIndexing { get; set; }

	}
}