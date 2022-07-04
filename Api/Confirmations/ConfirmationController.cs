using Microsoft.AspNetCore.Mvc;

namespace Api.Confirmations
{
    /// <summary>Handles confirmation endpoints.</summary>
    [Route("v1/confirmation")]
	public partial class ConfirmationController : AutoController<Confirmation>
    {
    }
}