namespace Api.Pages
{
	
	/// <summary>
	/// Page metadata for the native mobile page.
	/// </summary>
	public class MobilePageMeta
	{
		/// <summary>
		/// Api host.
		/// </summary>
		public string ApiHost;

		/// <summary>
		/// Locale ID
		/// </summary>
		public uint LocaleId = 1;

		/// <summary>
		/// Includes cordova.js
		/// </summary>
		public bool Cordova = true;
		
		/// <summary>
		/// Includes all non-admin pages
		/// </summary>
		public bool IncludePages = true;

		/// <summary>
		/// Custom JS to include on the mobile page.
		/// </summary>
		public string CustomJs;
	}
	
}