using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.AutoForms;
using Api.Contexts;
using Api.Database;
using Api.Startup;
using Api.Users;

namespace Api.Permissions
{
    /// <summary>
    /// A configurable role.
    /// </summary>
    public partial class UserRole : VersionedContent<uint>
	{
		/// <summary>
		/// The nice name of the role, usually in the site default language.
		/// </summary>
        public string Name;
		/// <summary>
		///  The role key - usually the lowercase, underscores instead of spaces variant of the first set name.
		///  This shouldn't change after it has been set.
		/// </summary>
        public string Key;
		
		/// <summary>
		/// True if this role can view the admin panel.
		/// </summary>
		public bool CanViewAdmin;
		
		/// <summary>
		/// If this role can view the admin panel, this is the content of what it sees on the admin dashboard.
		/// </summary>
		public string DashboardJson;
		
		/// <summary>
		/// The raw grant rules for this user role.
		/// </summary>
		[Module("Admin/PermissionGrid")]
		[Data("editor", "true")]
		public string GrantRuleJson;
	}
}