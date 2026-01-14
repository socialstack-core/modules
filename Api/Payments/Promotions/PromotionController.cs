using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles promotion endpoints.</summary>
    [Route("v1/promotion")]
	public partial class PromotionController : AutoController<Promotion>
    {
    }
}