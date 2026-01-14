using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles productTemplate endpoints.</summary>
    [Route("v1/productTemplate")]
	public partial class ProductTemplateController : AutoController<ProductTemplate>
    {
    }
}