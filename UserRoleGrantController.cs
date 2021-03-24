using Microsoft.AspNetCore.Mvc;

namespace Api.Permissions
{
    /// <summary>Handles user role grant endpoints.</summary>
    [Route("v1/userrolegrant")]
	public partial class UserRoleGrantController : AutoController<UserRoleGrant>
    {
    }
}