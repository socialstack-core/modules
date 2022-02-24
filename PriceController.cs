using Microsoft.AspNetCore.Mvc;

namespace Api.Prices
{
    /// <summary>Handles price endpoints.</summary>
    [Route("v1/price")]
	public partial class PriceController : AutoController<Price>
    {
    }
}