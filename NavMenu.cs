using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;

namespace Api.NavMenus
{
	
	/// <summary>
	/// A particular nav menu.
	/// </summary>
	public partial class NavMenu : VersionedContent<uint>
	{
		/// <summary>
		/// A key used to identify a menu by its purpose.
		/// E.g. "primary" or "admin_primary"
		/// </summary>
		public string Key;

		/// <summary>
		/// The name of the menu in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;
	}
	
}