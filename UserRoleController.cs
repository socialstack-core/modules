using Microsoft.AspNetCore.Mvc;

namespace Api.Permissions
{
    /// <summary>Handles user role endpoints.</summary>
    [Route("v1/userrole")]
	public partial class UserRoleController : AutoController<UserRole>
    {
    }
}