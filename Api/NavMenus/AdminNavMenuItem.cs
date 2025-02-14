using Api.Database;
using Api.Permissions;
using Api.Startup;

namespace Api.NavMenus
{
	
	/// <summary>
	/// A particular entry within a navigation menu.
	/// </summary>
	public partial class AdminNavMenuItem : Content<uint>
	{
		/// <summary>
		/// The title of this nav menu entry.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;

		/// <summary>
		/// Often a URL but is be whatever the item wants to emit when it's clicked.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Target;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		[DatabaseField(Length = 100)]
		public string IconRef;

		/// <summary>
		/// Enforce visibility rules.
		/// </summary>
		public string VisibilityRuleJson = null;
	}

}