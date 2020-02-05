using System;
using Api.Database;
using Api.Users;

namespace Api.Pages
{
	
	/// <summary>
	/// A page.
	/// </summary>
	public partial class Page : RevisionRow
	{
		/// <summary>
		/// The URL for this page.
		/// </summary>
		public string Url;
		
		/// <summary>
		/// The default title for this page.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// The pages content (as canvas JSON).
		/// </summary>
		public string BodyJson;
	}
	
}