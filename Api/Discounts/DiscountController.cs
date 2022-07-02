using Microsoft.AspNetCore.Mvc;

namespace Api.Discounts
{
    /// <summary>Handles discount endpoints.</summary>
    [Route("v1/discount")]
	public partial class DiscountController : AutoController<Discount>
    {
    }
}