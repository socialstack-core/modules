using System;
using System.Collections.Generic;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.Permissions;


namespace Api.NavMenus
{
	
	/// <summary>
	/// A particular entry within a navigation menu.
	/// </summary>
	public partial class AdminNavMenuItem : IHaveId<int>, IHaveRoleRestrictions
	{
		/// <summary>
		/// Entry ID.
		/// </summary>
		public int Id;
		
		/// <summary>
		/// The title of this nav menu entry.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// Often a URL but is be whatever the item wants to emit when it's clicked.
		/// </summary>
		public string Target;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		public string IconRef;
		
		/// <summary>
		/// Visible to dev role by default
		/// </summary>
		public bool VisibleToRole1 = true;
		
		/// <summary>
		/// Visible to admin role by default
		/// </summary>
		public bool VisibleToRole2 = true;
		
		/// <summary>
		/// Gets the entry ID.
		/// </summary>
		public int GetId()
		{
			return Id;
		}

		/// <summary>
		/// Sets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public void SetId(int id)
		{
			Id = id;
		}

	}

}