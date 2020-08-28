using Microsoft.AspNetCore.Mvc;

namespace Api.UserFlags
{
    /// <summary>Handles userFlag endpoints.</summary>
    [Route("v1/userFlag")]
	public partial class UserFlagController : AutoController<UserFlag>
    {
    }
}