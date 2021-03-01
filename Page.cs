using System;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;

namespace Api.Pages
{
	
	/// <summary>
	/// A page.
	/// </summary>
	public partial class Page : RevisionRow, IHaveRoleRestrictions
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
		public string BodyJson;

		/// <summary>
		/// The default description for this page.
		/// </summary>
		[Localized]
		public string Description;
		
		/// <summary>
		/// Page visibility varies when anon users.
		/// </summary>
		public bool VisibleToRole0 = true;
		
		/// <summary>
		/// Page visibility varies when guest user.
		/// </summary>
		public bool VisibleToRole3 = true;
		
		/// <summary>
		/// Page visibility varies when member user.
		/// </summary>
		public bool VisibleToRole4 = true;

	}
	
}