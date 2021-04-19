using Api.Permissions;
using Api.Users;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Users
{
	/// <summary>
	/// Implement this interface on a type to add automatic UserProfile support.
	/// Note that UserProfile objects have a reference to the full user data if you need it - it's just not serialized.
	/// </summary>
	public partial interface IHaveCreatorUser
    {
		/// <summary>
		/// The ID of the creator user.
		/// </summary>
		uint GetCreatorUserId();
	}
}
