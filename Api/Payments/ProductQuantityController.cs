using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles productUsage endpoints.</summary>
    [Route("v1/productUsage")]
	public partial class ProductQuantityController : AutoController<ProductQuantity>
    {
    }
}