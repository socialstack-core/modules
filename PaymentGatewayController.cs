using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.PaymentGateways
{
    /// <summary>Handles paymentGateway endpoints.</summary>
    [Route("v1/paymentGateway")]
	public partial class PaymentGatewayController : AutoController<PaymentGateway>
    {
        /// <summary>
		/// Creates a stripe payment intent.
		/// </summary>
		/// <returns></returns>
        [HttpPost("stripe/create-payment-intent")]
        public async ValueTask<PaymentIntentResponse> CreateStripePaymentIntent(PaymentIntentCreateRequest request)
        {
            if (request.Products == null || request.Products.Count == 0)
            {
                throw new PublicException("No products are being purchased", "no_products");
            }

            var context = await Request.GetContext();

            var result = await (_service as PaymentGatewayService).CreateStripePaymentIntent(context, request);

            return result;
        }
    }
}