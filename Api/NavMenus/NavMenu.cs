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

        /// <summary>
        /// Often a URL but is be whatever the item wants to emit when it's clicked.
		/// NB: Only applies if this menu has no subitems
        /// </summary>
        [DatabaseField(Length = 300)]
        public string Target;

        /// <summary>
        /// Optional sort order. Higher numbers list first.
        /// </summary>
        public int Order;
    }

}