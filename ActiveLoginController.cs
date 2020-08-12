using Microsoft.AspNetCore.Mvc;

namespace Api.ActiveLogins
{
    /// <summary>Handles activeLogin endpoints.</summary>
    [Route("v1/activeLogin")]
	public partial class ActiveLoginController : AutoController<ActiveLogin>
    {
    }
}