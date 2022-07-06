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
    [ApiController]
    public partial class PaymentGatewayController : ControllerBase
    {
        private PaymentGatewayService _service;

        /// <summary>
        /// Instanced automatically.
        /// </summary>
        /// <param name="service">Auto injected service.</param>
        public PaymentGatewayController(PaymentGatewayService service)
        {
            _service = service;
        }

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

            var result = await _service.CreateStripePaymentIntent(context, request);

            return result;
        }

        /// <summary>
		/// Creates a stripe setup intent.
		/// </summary>
		/// <returns></returns>
        [HttpGet("stripe/create-setup-intent")]
        public async ValueTask<SetupIntentResponse> CreateStripeSetupIntent()
        {
            var context = await Request.GetContext();

            var result = await _service.CreateStripeSetupIntent(context);

            return result;
        }

        /// <summary>
		/// Gets stripe payment methods.
		/// </summary>
		/// <returns></returns>
        [HttpGet("stripe/get-payment-methods")]
        public async ValueTask<PaymentMethodsResponse> GetStripePaymentMethods()
        {
            var context = await Request.GetContext();

            var result = await _service.GetStripePaymentMethods(context);

            return result;
        }

        /// <summary>
		/// Gets a specific stripe payment method.
		/// </summary>
		/// <returns></returns>
        [HttpPost("stripe/get-payment-method")]
        public async ValueTask<PaymentMethodResponse> GetStripePaymentMethod(PaymentMethodRequest request)
        {
            var context = await Request.GetContext();

            var result = await _service.GetStripePaymentMethod(context, request);

            return result;
        }
    }
}