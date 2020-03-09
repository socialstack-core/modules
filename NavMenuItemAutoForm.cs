using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.NavMenuItems
{
    /// <summary>
    /// Used when creating or updating a nav menu
    /// </summary>
    public partial class NavMenuItemAutoForm : AutoForm<NavMenuItem>
    {
		/// <summary>
		/// The ID of the nav menu this item belongs to.
		/// </summary>
		public int NavMenuId;

		/// <summary>
		/// ID of a parent nav menu item, if this is on a submenu. Null otherwise.
		/// </summary>
		public int? ParentItemId;

		/// <summary>
		/// The visual content of this menu item. Can contain imagery etc.
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// Often a URL but is be whatever the item wants to emit when it's clicked.
		/// </summary>
		public string Target;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		public string IconRef;
	}
}
