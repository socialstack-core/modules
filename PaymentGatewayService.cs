using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Stripe;
using Api.Startup;
using Api.Purchases;

namespace Api.PaymentGateways
{
	/// <summary>
	/// Handles paymentGateways.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PaymentGatewayService : AutoService<PaymentGateway>
    {
		private PaymentGatewayConfig _config;
        private PurchaseService _pruchases;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PaymentGatewayService(PurchaseService purchases) : base(Eventing.Events.PaymentGateway)
        {
            _pruchases = purchases;
			_config = GetConfig<PaymentGatewayConfig>();
		}

        /// <summary>
		/// Creates a stripe payment intent.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<PaymentIntentResponse> CreateStripePaymentIntent(Context context, PaymentIntentCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(_config.StripeSecretKey))
            {
                throw new PublicException("Stripe is not configured correctly", "no_secret_key");
            }

            var cost = CalculateOrderAmount(request.Products);

            if (cost <= 0)
            {
                throw new PublicException("Cannot create payment intent, cost needs to be a positive number greater than 0", "bad_cost");
            }

            var user = context.User;

            if (user == null)
            {
                throw new PublicException("You need to be logged in to preform this action", "no_user");
            }

            var purchase = await _pruchases.CreatePurchase(context, request, cost, _config.PaymentCurrency);

            if (purchase == null)
            {
                throw new PublicException("Something has gone wrong. The purchase was not created so payment will not be taken", "no_purchase");
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
                ReceiptEmail = user.Email,
                Metadata = new Dictionary<string, string> {
                    { "Purchase Id", purchase.Id.ToString() },
                    { "User Id", user.Id.ToString() },
                    { "User First Name", user.FirstName },
                    { "User Last Name", user.LastName }
                }
            });

            // update purchase with Id from payment intent
            if(await _pruchases.StartUpdate(context, purchase))
            {
                purchase.ThirdPartyId = paymentIntent.Id;
                
                purchase = await _pruchases.FinishUpdate(context, purchase);
            }

            return new PaymentIntentResponse { ClientSecret = paymentIntent.ClientSecret, PurchaseId = purchase.Id };
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
