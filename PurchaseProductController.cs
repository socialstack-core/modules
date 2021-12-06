using Microsoft.AspNetCore.Mvc;

namespace Api.PurchaseProducts
{
    /// <summary>Handles purchaseProduct endpoints.</summary>
    [Route("v1/purchaseProduct")]
	public partial class PurchaseProductController : AutoController<PurchaseProduct>
    {
    }
}