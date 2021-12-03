using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Stripe;
using Api.Startup;

namespace Api.PaymentGateways
{
	/// <summary>
	/// Handles paymentGateways.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PaymentGatewayService : AutoService<PaymentGateway>
    {
		private PaymentGatewayConfig _config;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PaymentGatewayService() : base(Eventing.Events.PaymentGateway)
        {
			_config = GetConfig<PaymentGatewayConfig>();
		}

        /// <summary>
		/// Creates a stripe payment intent.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<PaymentIntentResponse> CreateStripePaymentIntent(PaymentIntentCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(_config.StripeSecretKey))
            {
                throw new PublicException("Stripe is not configured correctly", "no_secret_key");
            }

			var paymentIntentService = new PaymentIntentService();
            StripeConfiguration.ApiKey = _config.StripeSecretKey;

            var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = CalculateOrderAmount(request.Products),
                Currency = _config.PaymentCurrency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            });

            // todo: save { userId, products, amount } to db? socialstack generate API/Purchases?

            return new PaymentIntentResponse { ClientSecret = paymentIntent.ClientSecret };
        }

        private long CalculateOrderAmount(List<Api.Products.Product> products)
        {
            long cost = 0;

            foreach(var product in products)
            {
                cost += product.SingleCostPence;
            }

            return cost;
        }
	}
    
}
