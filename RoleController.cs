using Microsoft.AspNetCore.Mvc;

namespace Api.Permissions
{
    /// <summary>Handles user role endpoints.</summary>
    [Route("v1/role")]
	public partial class RoleController : AutoController<Role>
    {
    }
}