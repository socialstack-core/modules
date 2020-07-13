using Microsoft.AspNetCore.Mvc;

namespace Api.PasswordResetRequests
{
    /// <summary>Handles passwordResetRequest endpoints.</summary>
    [Route("v1/passwordResetRequest")]
	public partial class PasswordResetRequestController : AutoController<PasswordResetRequest>
    {
    }
}