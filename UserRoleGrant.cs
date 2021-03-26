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
    public partial class UserRoleGrant : VersionedContent<int>
	{
		/// <summary>
		/// The ID of the role this is for.
		/// </summary>
		public int UserRoleId;
		
		/// <summary>
		/// The capability this is for.
		/// </summary>
		public string Capability;
		
		/// <summary>
		/// The grant rule, expressed in JSON. If this is NULL (or more generally, if the row doesn't event exist), 
		/// the value will be inherited from the parent role(s) will be used instead.
		/// </summary>
		public string GrantRuleJson;
	}
}