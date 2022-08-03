using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles subscription endpoints.</summary>
    [Route("v1/subscription")]
	public partial class SubscriptionController : AutoController<Subscription>
    {
    }
}