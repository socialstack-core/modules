using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Permissions
{
	/// <summary>
	/// Implement this interface on a type to add role restriction support.
	/// Add fields of the form public bool VisibleToRole4 = true; to your type where '4' is the Role ID.
	/// If you don't restrict a particular role, the default value is true.
	/// Note that this also just extends any other permission rules, so if the whole list etc is not visible to a role, this won't make it visible.
	/// </summary>
	public partial interface IHaveRoleRestrictions
    {
	}
}
