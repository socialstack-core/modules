using System;
using System.Collections.Generic;
using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.NavMenuItems
{
	
	/// <summary>
	/// A particular entry within a navigation menu.
	/// </summary>
	public partial class NavMenuItem : RevisionRow
	{
		/// <summary>
		/// The ID of the nav menu this item belongs to.
		/// </summary>
		public int NavMenuId;

		/// <summary>
		/// The key value of the host menu. Used to find these items quicker.
		/// </summary>
		public string MenuKey;

		/// <summary>
		/// ID of a parent nav menu item, if this is on a submenu. Null otherwise.
		/// </summary>
		public int? ParentItemId;

		/// <summary>
		/// The visual content of this menu item. Can contain imagery etc.
		/// </summary>
		[DatabaseField(Length = 400)]
		[Localized]
		public string BodyJson;

		/// <summary>
		/// Often a URL but is be whatever the item wants to emit when it's clicked.
		/// </summary>
		[DatabaseField(Length = 300)]
		public string Target;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}
	
}