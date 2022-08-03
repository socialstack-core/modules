using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles purchase endpoints.</summary>
    [Route("v1/purchase")]
	public partial class PurchaseController : AutoController<Purchase>
    {
    }
}