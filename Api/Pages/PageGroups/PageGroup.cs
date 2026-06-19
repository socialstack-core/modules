using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Pages
{
	
	/// <summary>
	/// A PageGroup
	/// </summary>
	public partial class PageGroup : VersionedContent<uint>
	{
		/// <summary>
		/// Pages added to this group will get given this URL prefix, followed then by a url-ified version of the page name.
		/// </summary>
		public string Url;
		
        /// <summary>
        /// The name of the pageGroup
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;
	}

}