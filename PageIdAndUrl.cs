namespace Api.Pages
{
	
	/// <summary>
	/// A minimalist variant of page. Used when building pregenerated pages - they no longer download the complete page list, but rather just this URL info.
	/// </summary>
	public struct PageIdAndUrl
	{
		/// <summary>
		/// Page URL (with tokens in it).
		/// </summary>
		public string Url;
		
		/// <summary>
		/// Page ID that the above URL is from.
		/// </summary>
		public uint PageId;
	}
	
}