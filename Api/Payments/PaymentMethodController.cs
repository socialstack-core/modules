using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles paymentMethod endpoints.</summary>
    [Route("v1/paymentMethod")]
	public partial class PaymentMethodController : AutoController<PaymentMethod>
    {
    }
}