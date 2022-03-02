using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.PaymentGateways;
using Api.CanvasRenderer;
using Stripe;
using Api.Startup;

namespace Api.Prices
{
	/// <summary>
	/// Handles stripeProductPrices.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PriceService : AutoService<Price>
	{
		PaymentGatewayConfig _paymentGatewayConfig;
		FrontendCodeService _frontEndCode;
		Products.ProductService _products;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PriceService(FrontendCodeService frontEndCode, Products.ProductService products) : base(Eventing.Events.Price)
		{
			InstallAdminPages("Prices", "fa:fa-dollar-sign", new string[] { "id", "name" });

			_frontEndCode = frontEndCode;
			_products = products;
			_paymentGatewayConfig = GetConfig<PaymentGatewayConfig>();

			Eventing.Events.Price.BeforeCreate.AddEventListener(async (Context ctx, Price price) => {

				if (price == null)
				{
					return price;
				}

				if (price.CostPence < 0)
				{
					throw new PublicException("Cost Pence must be greater than or equal to 0", "bad_cost");
				}

				return price;
			});

			Eventing.Events.Price.AfterCreate.AddEventListener(async (Context ctx, Price price) => {

				if (price == null)
				{
					return price;
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					var stripePrice = await CreateStripePrice(ctx, price, stripeSecret);

					if (await StartUpdate(ctx, price, DataOptions.IgnorePermissions))
					{
						price.StripePriceId = stripePrice.Id;

						price = await FinishUpdate(ctx, price);
					}
				}

				return price;
			});

			Eventing.Events.Price.BeforeUpdate.AddEventListener(async (Context ctx, Price price) => {

				if (price == null)
				{
					return price;
				}

				if (price.CostPence < 0)
				{
					throw new PublicException("Cost Pence must be greater than or equal to 0", "bad_cost");
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					StripeConfiguration.ApiKey = stripeSecret;

					var currentPrice = await Get(ctx, price.Id);
					var stripePrices = new Stripe.PriceService();
					var noStripePrice = string.IsNullOrEmpty(price.StripePriceId);

					if (noStripePrice
						|| currentPrice.CostPence != price.CostPence 
						|| currentPrice.ProductId != price.ProductId 
						|| currentPrice.IsRecurring != price.IsRecurring 
						|| currentPrice.IsMetered != price.IsMetered 
						|| currentPrice.RecurringPaymentIntervalMonths != price.RecurringPaymentIntervalMonths)
                    {
						var updateOptions = new PriceUpdateOptions
						{
							Active = false
						};

						var stripePrice = !noStripePrice ? await stripePrices.UpdateAsync(price.StripePriceId, updateOptions) : null;

						stripePrice = await CreateStripePrice(ctx, price, stripeSecret);

						price.StripePriceId = stripePrice.Id;
					}
					else if (currentPrice.Name != price.Name)
                    {
						var updateOptions = new PriceUpdateOptions
						{
							Nickname = price.Name
						};

						var stripePrice = await stripePrices.UpdateAsync(price.StripePriceId, updateOptions);	
					}
				}

				return price;
			});

			Eventing.Events.Price.AfterDelete.AddEventListener(async (Context ctx, Price price) => {

				if (price == null)
				{
					return price;
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					if (!string.IsNullOrEmpty(price.StripePriceId))
					{
						StripeConfiguration.ApiKey = stripeSecret;

						var updateOptions = new PriceUpdateOptions
						{
							Active = false
						};

						var stripePrices = new Stripe.PriceService();
						var stripePrice = await stripePrices.UpdateAsync(price.StripePriceId, updateOptions);
					}
				}

				return price;
			});
		}

		/// <summary>
		/// Creates a price in stripe for the given local price
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="price"></param>
		/// <param name="stripeSecret"></param>
		/// <returns></returns>
		public async Task<Stripe.Price> CreateStripePrice(Context ctx, Price price, string stripeSecret)
        {
			//create stripe product and price
			StripeConfiguration.ApiKey = stripeSecret;
			var hostname = _frontEndCode.GetPublicUrl().Replace("https://", "").Replace("http://", "");

			var product = await _products.Get(ctx, price.ProductId, DataOptions.IgnorePermissions);

			var stripeProductId = product?.StripeProductId;

			if (string.IsNullOrEmpty(stripeProductId))
            {
				throw new PublicException("Price does not have a matching stripe product", "bad_price");
            }

			var metadata = new Dictionary<string, string> {
						{ "Price Id", price.Id.ToString() },
						{ "Parent App Hostname" , hostname }
					};

			var priceOptins = new PriceCreateOptions
			{
				Nickname = price.Name,
				UnitAmount = price.CostPence,
				Currency = "gbp",
				Product = stripeProductId,
				Recurring = price.IsRecurring ? new PriceRecurringOptions
				{
					Interval = "month",
					IntervalCount = price.RecurringPaymentIntervalMonths > 0 ? price.RecurringPaymentIntervalMonths : 1,
					UsageType = price.IsMetered ? "metered" : "licensed"
				} : null,
				Metadata = metadata
			};

			var stripePrices = new Stripe.PriceService();
			var stripePrice = await stripePrices.CreateAsync(priceOptins);

			return stripePrice;
		}
	}

}
