using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Permissions
{
	/// <summary>
	/// Implement this interface on a type to add user restriction support.
	/// Unlike IHaveRoleRestrictions, this interface restricts access by particular user.
	/// Note that the restrictions can actually be by any context object.
	/// For example, if Company is part of the context then you can restrict by company as well.
	/// Note that 
	/// </summary>
	public partial interface IHaveUserRestrictions
    {
		
		/// <summary>
		/// True if user restrictions are active for this object.
		/// Some objects vary in that they have public and private modes.
		/// If in doubt, return true always.
		/// </summary>
		bool UserRestrictionsActive{
			get;
		}
		
		/// <summary>
		/// True if the PermittedUsers field should be set. This is so you can, for example, display a list of people in a group chat.
		/// However in instances where the membership should be private/ hidden, return false.
		/// </summary>
		bool PermittedUsersListVisible{
			get;
		}
		
		/// <summary>
		/// Often a list of UserProfile objects, but can be mixed if you need it to be.
		/// Can also include entries that are invites or were rejected.
		/// They are always Context objects though.
		/// For example, companies are private messaging each other. A company is actually a group of people. 
		/// The company a user is logged in as must be part of their context
		/// in order to be able to successfully permit the company.
		/// </summary>
		List<PermittedContent> PermittedUsers {get; set;}
		
	}
}
