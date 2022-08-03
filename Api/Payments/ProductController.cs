using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles product endpoints.</summary>
    [Route("v1/product")]
	public partial class ProductController : AutoController<Product>
    {
    }
}