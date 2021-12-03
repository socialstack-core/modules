using Microsoft.AspNetCore.Mvc;

namespace Api.Products
{
    /// <summary>Handles product endpoints.</summary>
    [Route("v1/product")]
	public partial class ProductController : AutoController<Product>
    {
    }
}