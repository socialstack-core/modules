using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments
{
	/// <summary>
	/// Handles productQuantities.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductQuantityService : AutoService<ProductQuantity>
    {
		private ProductService _products;
		private PriceService _prices;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductQuantityService(ProductService products, PriceService prices) : base(Events.ProductQuantity)
        {
			_products = products;
			_prices = prices;


			/*
			Events.Service.AfterStart.AddEventListener(async (Context ctx, object s) => {
				var context = new Context(1, 1, 1);

				// - Standard Tiers example -

				System.Console.WriteLine("");
				System.Console.WriteLine("- Standard Tiers -");
				System.Console.WriteLine("");

				var baseProduct = await _products.Get(context, 500000);

				if (baseProduct == null)
				{
					// Base price
					await _prices.Create(context, new Price()
					{
						Id = 500000,
						CurrencyCode = "GBP",
						Amount = 100
					}, DataOptions.IgnorePermissions);
					
					baseProduct = await _products.Create(context, new Product()
					{
						Id = 500000,
						Name = "Bars of Chocolate",
						PriceId = 500000,
						PriceStrategy = 0
					}, DataOptions.IgnorePermissions);

					// 10% off for 3 or more
					await _prices.Create(context, new Price()
					{
						Id = 500001,
						CurrencyCode = "GBP",
						Amount = 90
					}, DataOptions.IgnorePermissions);
					await _products.Create(context, new Product()
					{
						Id = 500001,
						Name = "Bars of Chocolate, 10% off",
						PriceId = 500001,
						MinQuantity = 3
					}, DataOptions.IgnorePermissions);

					// 15% off for 10 or more
					await _prices.Create(context, new Price()
					{
						Id = 500002,
						CurrencyCode = "GBP",
						Amount = 85
					}, DataOptions.IgnorePermissions);
					await _products.Create(context, new Product()
					{
						Id = 500002,
						Name = "Bars of Chocolate, 15% off",
						PriceId = 500002,
						MinQuantity = 10
					}, DataOptions.IgnorePermissions);

					// Create the mappings:
					await _products.EnsureMapping(context, baseProduct, _products, new uint[] { 500001, 500002 }, "Tiers");
				}

				try
				{
					for (var i = 0; i < 12; i++)
					{
						var cost = await GetCostOf(new ProductQuantity()
						{
							ProductId = 500000,
							Quantity = (ulong)i
						}, 1);

						System.Console.WriteLine(i + "\t" + ((double)cost.Amount / 100d) + cost.CurrencyCode);

					}
				}
				catch (System.Exception e)
				{
					System.Console.WriteLine(e.ToString());
				}


				// - Step once example -

				System.Console.WriteLine("");
				System.Console.WriteLine("- Step Once -");
				System.Console.WriteLine("");

				baseProduct = await _products.Get(context, 500010);

				if (baseProduct == null)
				{
					// Base price
					await _prices.Create(context, new Price()
					{
						Id = 500010,
						CurrencyCode = "GBP",
						Amount = 1
					}, DataOptions.IgnorePermissions);

					baseProduct = await _products.Create(context, new Product()
					{
						Id = 500010,
						Name = "Mobile data, £10 for 1000GB",
						PriceId = 500010,
						PriceStrategy = 1,
						MinQuantity = 1000
					}, DataOptions.IgnorePermissions);

					// 10p per GB overage
					await _prices.Create(context, new Price()
					{
						Id = 500011,
						CurrencyCode = "GBP",
						Amount = 10
					}, DataOptions.IgnorePermissions);
					await _products.Create(context, new Product()
					{
						Id = 500011,
						Name = "Mobile data, 10p per GB overage",
						PriceId = 500011,
						MinQuantity = 1001
					}, DataOptions.IgnorePermissions);

					// 15% off for 10 or more
					await _prices.Create(context, new Price()
					{
						Id = 500012,
						CurrencyCode = "GBP",
						Amount = 9
					}, DataOptions.IgnorePermissions);
					await _products.Create(context, new Product()
					{
						Id = 500012,
						Name = "Mobile data, 10% overage discount",
						PriceId = 500012,
						MinQuantity = 2000
					}, DataOptions.IgnorePermissions);

					// Create the mappings:
					await _products.EnsureMapping(context, baseProduct, _products, new uint[] { 500011, 500012 }, "Tiers");
				}

				var quants = new ulong[] { 100, 500, 1000, 1001, 1500, 1999, 2000, 2500 };

				try
				{
					for (var i = 0; i < quants.Length; i++)
					{
						var cost = await GetCostOf(new ProductQuantity()
						{
							ProductId = 500010,
							Quantity = quants[i]
						}, 1);

						System.Console.WriteLine(quants[i] + "\t" + ((double)cost.Amount / 100d) + cost.CurrencyCode);

					}
				}
				catch (System.Exception e)
				{
					System.Console.WriteLine(e.ToString());
				}

				// - Step always example -

				System.Console.WriteLine("");
				System.Console.WriteLine("- Step Always -");
				System.Console.WriteLine("");

				baseProduct = await _products.Get(context, 500020);

				if (baseProduct == null)
				{
					// Base price
					baseProduct = await _products.Create(context, new Product()
					{
						Id = 500020,
						Name = "Mobile data, £10 for 1000GB (Step always)",
						PriceId = 500010,
						PriceStrategy = 2,
						MinQuantity = 1000
					}, DataOptions.IgnorePermissions);

					// Create the mappings:
					await _products.EnsureMapping(context, baseProduct, _products, new uint[] { 500011, 500012 }, "Tiers");
				}

				try
				{
					for (var i = 0; i < quants.Length; i++)
					{
						var cost = await GetCostOf(new ProductQuantity()
						{
							ProductId = 500020,
							Quantity = quants[i]
						}, 1);

						System.Console.WriteLine(quants[i] + "\t" + ((double)cost.Amount / 100d) + cost.CurrencyCode);

					}
				}
				catch (System.Exception e)
				{
					System.Console.WriteLine(e.ToString());
				}

				return s;
			});
			*/
		}

		/// <summary>
		/// Gets the cost of a ProductQuantity in the given locale.
		/// </summary>
		/// <param name="productQuantity"></param>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public async ValueTask<ProductCost> GetCostOf(ProductQuantity productQuantity, uint localeId)
		{
			return await GetCostOf(new Context(localeId, 0, 0), productQuantity);
		}

		/// <summary>
		/// Gets the cost of a ProductQuantity using the locale in the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="productQuantity"></param>
		/// <returns></returns>
		public async ValueTask<ProductCost> GetCostOf(Context context, ProductQuantity productQuantity)
		{
			if (productQuantity == null)
			{
				return ProductCost.None;
			}

			var quantity = productQuantity.Quantity;
			
			// Get the product:
			var product = await _products.Get(context, productQuantity.ProductId, DataOptions.IgnorePermissions);

			if (product == null)
			{
				return ProductCost.None;
			}

			var isSubscriptionProduct = product.BillingFrequency != 0;

			// First if the quantity is below the products base min, check with site config.
			if (quantity < product.MinQuantity)
			{
				if (_products.ErrorIfBelowMinimum)
				{
					throw new PublicException("Selected quantity is below the minimum.", "below_min");
				}
				else
				{ 
					// Round:
					quantity = product.MinQuantity;
				}
			}

			// Get the product tiers (null if there are none):
			var tiers = await _products.GetTiers(context, product);

			Product prodToUse = product;
			Price price;
			ulong totalCost;

			if (prodToUse.PriceId == 0)
			{
				throw new PublicException(
					"Product is not currently available in your currency. If you believe this is a mistake, please get in touch.",
					"not_available"
				);
			}

			// Get the base price:
			price = await _prices.Get(context, prodToUse.PriceId, DataOptions.IgnorePermissions);

			if (price == null)
			{
				throw new PublicException(
					"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
					"not_available"
				);
			}
			
			if (tiers != null && tiers.Count > 0)
			{
				// Which tier is the quantity going to target?
				int targetTier = -1;

				for (var i = tiers.Count - 1; i >= 0; i--)
				{
					if (quantity >= tiers[i].MinQuantity)
					{
						// Using this tiered product.
						targetTier = i;
						break;
					}
				}

				if (targetTier == -1)
				{
					// Within the base price. Doesn't matter what the price strategy is - this is always the same.

					// Total cost is:
					totalCost = quantity * price.Amount;

					// Watch out for overflows, just in case someone uses an
					// incredibly large quantity to try to get a lot for nothing:
					if (price.Amount != 0 && totalCost / price.Amount != quantity)
					{
						throw new PublicException(
							"The requested quantity is too large.",
							"substantial_quantity"
						);
					}
				}
				else
				{
					// See the wiki for details on pricing strategies.
					switch (product.PriceStrategy)
					{
						case 0:
							// Standard pricing strategy.

							// You pay the base product rate unless quantity passes any of the thresholds in the tiers.
							prodToUse = tiers[targetTier];

							// Get the price:
							price = await _prices.Get(context, prodToUse.PriceId, DataOptions.IgnorePermissions);

							if (price == null)
							{
								throw new PublicException(
									"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
									"not_available"
								);
							}

							// Total cost is:
							totalCost = quantity * price.Amount;

							// Watch out for overflows, just in case someone uses an
							// incredibly large quantity to try to get a lot for nothing:
							if (price.Amount != 0 && totalCost / price.Amount != quantity)
							{
								throw new PublicException(
									"The requested quantity is too large.",
									"substantial_quantity"
								);
							}

							break;
						case 1:
							// Step once.

							// You pay the base product rate unless quantity passes any of the thresholds in the tiers.

							// The step - we know we're above at least the threshold of the first tier.
							var excessThreshold = tiers[0].MinQuantity;

							// Add base number of products.
							totalCost = (excessThreshold - 1) * price.Amount;

							// Get the excess:
							var excess = quantity - (excessThreshold - 1);

							// Next establish which tier the price is in:
							prodToUse = tiers[targetTier];

							// Get the price:
							price = await _prices.Get(context, prodToUse.PriceId, DataOptions.IgnorePermissions);

							if (price == null)
							{
								throw new PublicException(
									"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
									"not_available"
								);
							}

							// Add excess number of items to the total, ensuring that we don't overflow.
							var excessCost = excess * price.Amount;

							if (price.Amount != 0 && excessCost / price.Amount != excess)
							{
								throw new PublicException(
									"The requested quantity is too large.",
									"substantial_quantity"
								);
							}

							var origTotal = totalCost;
							totalCost += excessCost;

							if (totalCost < origTotal)
							{
								throw new PublicException(
									"The requested quantity is too large.",
									"substantial_quantity"
								);
							}

							break;
						case 2:
							// Step always.

							// Base price first:
							excessThreshold = tiers[0].MinQuantity;

							// Add base number of products.
							totalCost = (excessThreshold - 1) * price.Amount;

							// Handle each fully passed tier next.
							for (var i = 0; i < targetTier; i++)
							{
								// The max amt for this tier is the following tiers min minus this tiers min.
								var tier = tiers[i];
								var max = tiers[i + 1].MinQuantity - tier.MinQuantity;

								price = await _prices.Get(context, tier.PriceId, DataOptions.IgnorePermissions);

								if (price == null)
								{
									throw new PublicException(
										"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
										"not_available"
									);
								}

								var tierTotal = max * price.Amount;
								// A singular tier is expected to never be so large that it always overflows.
								// Adding it on however might do so.
								var prevTotal = totalCost;
								totalCost += tierTotal;

								// Overflow check:
								if (totalCost < prevTotal)
								{
									throw new PublicException(
										"The requested quantity is too large.",
										"substantial_quantity"
									);
								}

							}

							// Handle any final excess.
							prodToUse = tiers[targetTier];
							excess = quantity - (prodToUse.MinQuantity - 1);

							price = await _prices.Get(context, prodToUse.PriceId, DataOptions.IgnorePermissions);

							if (price == null)
							{
								throw new PublicException(
									"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
									"not_available"
								);
							}

							excessCost = excess * price.Amount;

							if (price.Amount != 0 && excessCost / price.Amount != excess)
							{
								throw new PublicException(
									"The requested quantity is too large.",
									"substantial_quantity"
								);
							}

							origTotal = totalCost;
							totalCost += excessCost;

							if (totalCost < origTotal)
							{
								throw new PublicException(
									"The requested quantity is too large.",
									"substantial_quantity"
								);
							}

							break;
						default:
							throw new PublicException("Unknown pricing strategy", "pricing_strategy_notset");
					}
				}
			}
			else
			{
				// Just a simple price * qty.

				// Get the price:
				price = await _prices.Get(context, prodToUse.PriceId, DataOptions.IgnorePermissions);

				if (price == null)
				{
					throw new PublicException(
						"Product is not currently available in your currency as the price is unavailable. If you believe this is a mistake, please get in touch.",
						"not_available"
					);
				}

				// Total cost is:
				totalCost = quantity * price.Amount;

				// Watch out for overflows, just in case someone uses an
				// incredibly large quantity to try to get a lot for nothing:
				if (price.Amount != 0 && totalCost / price.Amount != quantity)
				{
					throw new PublicException(
						"The requested quantity is too large.",
						"substantial_quantity"
					);
				}

			}

			return new ProductCost()
			{
				CurrencyCode = price.CurrencyCode,
				Amount = totalCost,
				SubscriptionProducts = isSubscriptionProduct
			};
		}

	}
    
}
