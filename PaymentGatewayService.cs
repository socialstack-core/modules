using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Stripe;
using Api.Startup;
using Api.Purchases;
using System.Linq;

namespace Api.PaymentGateways
{
	/// <summary>
	/// Handles paymentGateways.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PaymentGatewayService : AutoService<PaymentGateway>
    {
		private PaymentGatewayConfig _config;
        private PurchaseService _purchases;
        private Api.Products.ProductService _products;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PaymentGatewayService(PurchaseService purchases, Api.Products.ProductService products) : base(Eventing.Events.PaymentGateway)
        {
            _purchases = purchases;
            _products = products;
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

            var products = await _products.Where("Id=[?]", DataOptions.IgnorePermissions).Bind(request.Products.Select(products => products.Id)).ListAll(context);

            var cost = CalculateOrderAmount(products, request.Products);

            if (cost <= 0)
            {
                throw new PublicException("Cannot create payment intent, cost needs to be a positive number greater than 0", "bad_cost");
            }

            var user = context.User;

            if (user == null)
            {
                throw new PublicException("You need to be logged in to preform this action", "no_user");
            }

            var purchase = await _purchases.CreatePurchase(context, request, cost, _config.PaymentCurrency, request.CustomReference);

            if (purchase == null)
            {
                throw new PublicException("Something has gone wrong. The purchase was not created so payment will not be taken", "no_purchase");
            }

			var paymentIntentService = new PaymentIntentService();
            StripeConfiguration.ApiKey = _config.StripeSecretKey;

            var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = cost,
                Currency = _config.PaymentCurrency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                ReceiptEmail = string.IsNullOrWhiteSpace(request.RecieptEmail) ? user.Email : request.RecieptEmail,
                Metadata = new Dictionary<string, string> {
                    { "Purchase Id", purchase.Id.ToString() },
                    { "User Id", user.Id.ToString() },
                    { "Custom Reference" , request.CustomReference}
                }
            });

            // update purchase with Id from payment intent
            if(await _purchases.StartUpdate(context, purchase, DataOptions.IgnorePermissions))
            {
                purchase.ThirdPartyId = paymentIntent.Id;
                
                purchase = await _purchases.FinishUpdate(context, purchase);
            }

            return new PaymentIntentResponse { ClientSecret = paymentIntent.ClientSecret, PurchaseId = purchase.Id };
        }

        private long CalculateOrderAmount(List<Api.Products.Product> rawProducts, List<IdQuantity> productQuantities)
        {
            long cost = 0;

            foreach(var productQuantity in productQuantities)
            {
                // Find the product by ID:
                var product = rawProducts.FirstOrDefault(p => p.Id == productQuantity.Id);

                if (product == null)
                {
                    throw new PublicException("Product does not exist with ID " + productQuantity.Id, "product_not_found");
                }

                cost += product.SingleCostPence * productQuantity.Quantity;
            }

            return cost;
        }
	}
    
}
