using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles purchaseProduct endpoints.</summary>
    [Route("v1/subscriptionusage")]
    public partial class SubscriptionUsageController : AutoController<SubscriptionUsage>
    {
    }
}