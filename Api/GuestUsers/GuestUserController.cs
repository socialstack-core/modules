using Microsoft.AspNetCore.Mvc;

namespace Api.GuestUsers
{
    /// <summary>Handles guestUser endpoints.</summary>
    [Route("v1/guestUser")]
	public partial class GuestUserController : AutoController<GuestUser>
    {
    }
}