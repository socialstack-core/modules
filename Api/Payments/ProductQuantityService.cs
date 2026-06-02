using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
		private CouponService _coupons;
		private ProductConfig _config;
		private CouponConfig _couponConfig;
		private PriceServiceConfig _priceConfig;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductQuantityService(ProductService products, PriceService prices, CouponService coupons) : base(Events.ProductQuantity)
        {
			_products = products;
			_prices = prices;
			_coupons = coupons;
			_config = GetConfig<ProductConfig>();
			_couponConfig = GetConfig<CouponConfig>();
			_priceConfig = GetConfig<PriceServiceConfig>();


			Events.Product.BeforeCreate.AddEventListener(async (Context context, Product product) =>
			{
				if (product == null)
				{
					return null;
				}

				var componentSet = product.GetTemporaryComponents();

				if (componentSet != null)
				{
					// 1. Create any that are needed
					List<ulong> mappings = new List<ulong>();

					foreach (var componentInfo in componentSet)
					{
						if (componentInfo.DeleteComponent)
						{
							// Ignore this one - it never existed anyway.
							continue;
						}

						if (componentInfo.Id == 0)
						{
							// Creating a new productQuantity. Following the same pattern as AutoController.

							// Using InstanceType such that it supports any dynamically added fields as well:
							var componentProductQuantity = (ProductQuantity)Activator.CreateInstance(InstanceType);
							await SetFieldsOnObject(componentProductQuantity, context, componentInfo.ProductQuantity);
							componentProductQuantity.UserId = context.UserId;

							// Not permitted to create with a specified ID via the API. Ensure it's 0:
							componentProductQuantity.SetId(default);

							// And the create call itself:
							componentProductQuantity = await Create(context, componentProductQuantity);

							if (componentProductQuantity != null)
							{
								mappings.Add(componentProductQuantity.Id);
							}
						}
					}

					// Finally update the mapping set.
					product.Mappings.Set("productcomponents", mappings);
				}

				return product;
			});



			Events.Product.BeforeUpdate.AddEventListener(async (Context context, Product toUpdate, Product original) =>
			{

				if (toUpdate == null)
				{
					return null;
				}

				var componentSet = toUpdate.GetTemporaryComponents();

				if (componentSet != null)
				{
					// 1. Create any that are needed
					// 2. Update "productComponent" Mappings
					// 3. Update ProductQuantity quantity
					// 4. Delete any that have been marked as such
					List<ulong> mappings = new List<ulong>();

					foreach (var componentInfo in componentSet)
					{
						if (componentInfo.DeleteComponent)
						{
							if (componentInfo.Id == 0)
							{
								// Ignore this one - it never existed anyway.
								continue;
							}

							// Delete it now:
							await Delete(context, componentInfo.Id);

							continue;
						}

						if (componentInfo.Id == 0)
						{
							// Creating a new productQuantity. Following the same pattern as AutoController.

							// Using InstanceType such that it supports any dynamically added fields as well:
							var componentProductQuantity = (ProductQuantity)Activator.CreateInstance(InstanceType);
							await SetFieldsOnObject(componentProductQuantity, context, componentInfo.ProductQuantity);
							componentProductQuantity.UserId = context.UserId;

							// Not permitted to create with a specified ID via the API. Ensure it's 0:
							componentProductQuantity.SetId(default);

							// And the create call itself:
							componentProductQuantity = await Create(context, componentProductQuantity);

							if (componentProductQuantity != null)
							{
								mappings.Add(componentProductQuantity.Id);
							}
						}
						else
						{
							// Update. Load the productQuantity such that we can set the changed fields on it,
							var originalEntity = await Get(context, componentInfo.Id);

							if (originalEntity == null)
							{
								continue;
							}

							if (componentInfo.UpdateComponent)
							{
								// and again like above we're just following what AutoController does.
								if (originalEntity == null)
								{
									continue;
								}

								var entityToUpdate = StartUpdate(context, originalEntity);
								await SetFieldsOnObject(entityToUpdate, context, componentInfo.ProductQuantity);

								// Make sure it's still the original ID:
								entityToUpdate.SetId(componentInfo.Id);

								entityToUpdate = await FinishUpdate(context, entityToUpdate, originalEntity);
							}

							mappings.Add(originalEntity.Id);
						}
					}

					// Finally update the mapping set.
					toUpdate.Mappings.Set("productcomponents", mappings);
				}


				return toUpdate;
			});



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
						Name = "Mobile data, �10 for 1000GB",
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
						Name = "Mobile data, �10 for 1000GB (Step always)",
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
		/// If the given collection has any errors in it, this throws.
		/// </summary>
		/// <param name="collection"></param>
		public void RequireNoErrors(ProductQuantityPricing collection)
		{
			if (collection == null)
			{
				return;
			}


			if (collection.ErrorCode != null)
			{
				throw new PublicException(collection.ErrorMessage, collection.ErrorCode);
			}

			if (collection.Contents == null)
			{
				return;
			}

			// Does any line item have an error code either?
			foreach (var lineItem in collection.Contents)
			{
				if (lineItem.ErrorCode != null)
				{
					throw new PublicException(lineItem.ErrorMessage, lineItem.ErrorCode);
				}
			}
		}

		/// <summary>
		/// Adds or removes N of the given product from the given set of product quantity IDs.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="productQuantityMapping"></param>
		/// <param name="product"></param>
		/// <param name="quantity"></param>
		/// <param name="blockCreate"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantity> ChangeItems(Context context, List<ulong> productQuantityMapping, Product product, CartQuantity quantity, bool blockCreate = false)
		{
			if (!quantity.Delta.HasValue && !quantity.Total.HasValue)
			{
				throw new PublicException("Please specify a quantity. It can either be a specific total quantity or a change in the quantity.", "quantity/missing");
			}

			// Check if this product is already in this set of items:
			var inSet = await Where("Id=[?]", DataOptions.IgnorePermissions)
				.Bind(productQuantityMapping.Select(id => (uint)id))
				.ListAll(context);
			var pQuantity = inSet.FirstOrDefault(item => item.ProductId == product.Id);

			ProductQuantity toRemove = null;

			if (pQuantity == null)
			{
				if (blockCreate)
				{
					// Not permitted to create.
					return null;
				}

				// New object.
				int currentQuantity = quantity.Delta.HasValue ? quantity.Delta.Value : (int)quantity.Total.Value;

				if (currentQuantity <= 0)
				{
					// No change.
					return null;
				}

				// Initial stock check. Note that a product can run out of stock whilst being in the cart.
				if (product.Stock != null)
				{
					if (!product.ContinueSellingWithNoStock && currentQuantity > product.Stock.Value)
					{
						throw new PublicException("Unfortunately there's not enough in stock to place your order.", "stock/insufficient");
					}
				}

				// Create a new one:
				pQuantity = await Create(context, new ProductQuantity()
				{
					ProductId = product.Id,
					Quantity = (uint)currentQuantity
				}, DataOptions.IgnorePermissions);

				productQuantityMapping.Add(pQuantity.Id);
			}
			else
			{
				// Add or subtract from the existing one.
				int newQty = quantity.Delta.HasValue ? (quantity.Delta.Value + (int)pQuantity.Quantity) : (int)quantity.Total.Value;

				if (newQty <= 0)
				{
					toRemove = pQuantity;
					await Delete(context, pQuantity, DataOptions.IgnorePermissions);

					// Remove from the mapping set:
					productQuantityMapping.Remove(pQuantity.Id);
				}
				else
				{
					// Initial stock check. Note that a product can run out of stock whilst being in the cart.
					if (product.Stock != null)
					{
						if (!product.ContinueSellingWithNoStock && newQty > product.Stock.Value)
						{
							throw new PublicException("Unfortunately there's not enough in stock to place your order.", "stock_insufficient");
						}
					}

					await Update(context, pQuantity, (Context ctx, ProductQuantity toUpdate, ProductQuantity orig) =>
					{
						toUpdate.Quantity = (uint)newQty;
					}, DataOptions.IgnorePermissions);

					// Ensure it's in the mapping set:
					if (productQuantityMapping.IndexOf(pQuantity.Id) == -1)
					{
						productQuantityMapping.Add(pQuantity.Id);
					}
				}
			}

			return pQuantity;
		}

		/// <summary>
		/// Gets pricing info for the given collection of product quantities, with an optional 
		/// coupon and in the given tax jurisdiction (if tax calc is active).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="productQuants"></param>
		/// <param name="couponId"></param>
		/// <param name="taxJurisdiction"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantityPricing> GetPricing(Context context, IEnumerable<ProductQuantity> productQuants, string taxJurisdiction, uint couponId)
		{
			// Comes from cache - resolves instantly:
			var locale = await context.GetLocale();

			var collection = new ProductQuantityPricing()
			{
				CouponId = couponId,
				TaxJurisdiction = taxJurisdiction,
				CurrencyCode = locale.CurrencyCode,
				Contents = new List<LineItem>()
			};

			ulong totalLessTax = 0;
			ulong total = 0;

			// Get tax calc if active (comes from lookup, resolves instantly):
			var taxCalc = await _prices.GetTaxCalculator(context, taxJurisdiction);

			if (productQuants != null)
			{
				foreach (var item in productQuants)
				{
					var quantity = item.Quantity;

					// Get the product:
					var product = await _products.Get(context, item.ProductId, DataOptions.IgnorePermissions);

					if (product == null)
					{
						// Product doesn't exist
						continue;
					}

					var cost = await GetCostOf(context, product, quantity, taxCalc);

					if (cost.SubscriptionProducts)
					{
						collection.HasSubscriptionProducts = true;
					}

					var lineItem = new LineItem()
					{
						ProductId = item.ProductId,
						Quantity = quantity,
						ProductQuantity = item,
						ProductQuantityId = item.Id,
						Product = product,
						Total = cost.Amount,
						ErrorCode = cost.ErrorCode,
						ErrorMessage = cost.ErrorMessage,
						TotalLessTax = cost.AmountLessTax
					};

					lineItem = await Events.Product.OnLineItem.Dispatch(context, lineItem);

					collection.Contents.Add(lineItem);

					totalLessTax += cost.AmountLessTax;
					total += cost.Amount;
				}
			}

			collection.TotalLessTax = totalLessTax;
			collection.TotalPDLessTax = totalLessTax;

			//Calculate the total when not using line by line calculations
			if(!_priceConfig.TaxLineByLine)
			{
				total = taxCalc.Apply(collection);
			}

			collection.Total = total;
			collection.TotalPD = total;

			// Valid coupon?
			if (couponId != 0)
			{
				var coupon = await _coupons.Get(context, couponId, DataOptions.IgnorePermissions);

				if (coupon != null && (coupon.Disabled || (coupon.ExpiryDateUtc.HasValue && coupon.ExpiryDateUtc.Value < System.DateTime.UtcNow)))
				{
					// NB: If max number of people is reached, it is marked as disabled.
					// Non-fatal error: the coupon just won't do anything.
					collection.ErrorMessage = "The provided coupon has expired.";
					collection.ErrorCode = "coupon_expired";

					// Clear the coupon.
					coupon = null;
				}

				// Next, factor in the coupon.
				if (coupon != null)
				{
					var priceContext = context;

					if (coupon.MinimumSpendAmount.TryGet(context, out uint minSpendAmount) && minSpendAmount > 0)
					{	
						// Are we above it?
						if (collection.Total < minSpendAmount)
						{
							// No!
							collection.ErrorMessage = "Can't use this coupon yet as the total is below the minimum spend.";
							collection.ErrorCode = "min_spend";
						}
					}

					if (coupon.DiscountPercent != 0)
					{
						var discountedTotal = collection.Total * (1d - ((double)coupon.DiscountPercent / 100d));

						if (discountedTotal <= 0)
						{
							// Becoming free!
							collection.Total = 0;
						}
						else
						{
							if(_couponConfig.DiscountRounding == RoundingMode.Up)
                            {
                                // Round the total down
								collection.Total = (ulong)Math.Floor(discountedTotal);
                            }
							else if(_couponConfig.DiscountRounding == RoundingMode.Nearest)
                            {
                                // Round to nearest pence/ cent
								collection.Total = (ulong)Math.Round(discountedTotal);
                            }
							else
                            {
                                // Round the total up by default
								collection.Total = (ulong)Math.Ceiling(discountedTotal);
                            }
							
						}

						discountedTotal = collection.TotalLessTax * (1d - ((double)coupon.DiscountPercent / 100d));

						if (discountedTotal <= 0)
						{
							// Becoming free!
							collection.TotalLessTax = 0;
						}
						else
						{
							if(_couponConfig.DiscountRounding == RoundingMode.Up)
                            {
								// Round the total down
								collection.TotalLessTax = (ulong)Math.Floor(discountedTotal);
							}
							else if(_couponConfig.DiscountRounding == RoundingMode.Nearest)
                            {
								// Round to nearest pence/ cent
								collection.TotalLessTax = (ulong)Math.Round(discountedTotal);
							}
							else
                            {
								// Round the total up by default
								collection.TotalLessTax = (ulong)Math.Ceiling(discountedTotal);
							}
						}
					}

					if (coupon.DiscountFixedAmount.TryGet(context, out uint discountFixedAmount) && discountFixedAmount > 0)
					{
						if (collection.Total < discountFixedAmount)
						{
							// Becoming free!
							collection.Total = 0;
						}
						else
						{
							// Discount a fixed number of units:
							collection.Total -= (ulong)discountFixedAmount;
						}

						if (collection.TotalLessTax < discountFixedAmount)
						{
							// Becoming free!
							collection.TotalLessTax = 0;
						}
						else
						{
							// Discount a fixed number of units:
							collection.TotalLessTax -= (ulong) discountFixedAmount;
						}
					}
				}

			}

			return collection;
		}

		/// <summary>
		/// Gets the cost of a ProductQuantity using the locale in the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="product"></param>
		/// <param name="quantity"></param>
		/// <param name="taxCalculator"></param>
		/// <returns></returns>
		private async ValueTask<ProductCost> GetCostOf(Context context, Product product, ulong quantity, TaxCalculator taxCalculator)
		{
			if (quantity == 0 || product == null)
			{
				return ProductCost.None;
			}

			var isSubscriptionProduct = product.BillingFrequency != 0;

			// Get the product price tiers:
			var priceComparison = await _products.GetPriceTiers(context, product);
			var tiers = priceComparison.TiersToUse;

			if (tiers == null || tiers.Count == 0)
			{
				// No configured prices.
				return ProductCost.None;
			}

			// Which tier is the quantity going to target?
			int targetTier = -1;

			for (var i = tiers.Count - 1; i >= 0; i--)
			{
				if (quantity >= tiers[i].MinimumQuantity)
				{
					// Using this tiered product.
					targetTier = i;
					break;
				}
			}

			var result = new ProductCost
			{
			};

			var belowMin = false;

			if (targetTier == -1)
			{
				// Below minimum.
				belowMin = true;
				targetTier = 0;
			}

			ulong totalCost;
			uint baseAmount;

			if (belowMin)
			{
				result.ErrorCode = "product/below_min";
				result.ErrorMessage = "Selected quantity is below the minimum.";
			}

			// See the wiki for details on pricing strategies.

			if (!tiers[0].Amount.TryGet(context, out baseAmount))
			{
				// Overrides min error.
				result.ErrorMessage = "Product is not currently available in your currency. If you believe this is a mistake, please get in touch.";
				result.ErrorCode = "product/not_available";
				return result;
			}

			if (product.PriceStrategy == 1 && tiers.Count > 1 && quantity >= tiers[1].MinimumQuantity)
			{
				// Step once strategy.

				// You pay the base product rate unless quantity passes any of the thresholds in the tiers.

				// The step - we know we're above at least the threshold of the first tier.
				var excessThreshold = tiers[1].MinimumQuantity;

				// Add base number of products.

				totalCost = (excessThreshold - 1) * baseAmount;

				// Get the excess:
				var excess = quantity - (excessThreshold - 1);

				if (!tiers[1].Amount.TryGet(context, out uint excessAmount))
				{
					// Overrides min error.
					result.ErrorMessage = "Product is not currently available in your currency. If you believe this is a mistake, please get in touch.";
					result.ErrorCode = "product/not_available";
					return result;
				}

				// Add excess number of items to the total, ensuring that we don't overflow.
				var excessCost = excess * excessAmount;
				if (excessAmount != 0 && excessCost / excessAmount != excess)
				{
					result.ErrorMessage = "The requested quantity is too large.";
					result.ErrorCode = "substantial_quantity";
					return result;
				}

				var origTotal = totalCost;
				totalCost += excessCost;

				if (totalCost < origTotal)
				{
					result.ErrorMessage = "The requested quantity is too large.";
					result.ErrorCode = "substantial_quantity";
					return result;
				}
			}
			else if (product.PriceStrategy == 2 && tiers.Count > 1 && quantity >= tiers[1].MinimumQuantity)
			{
				// Step always strategy.

				// Base price first:
				var excessThreshold = tiers[1].MinimumQuantity;

				// Add base number of products.
				totalCost = (excessThreshold - 1) * baseAmount;

				// Handle each fully passed tier next.
				for (var i = 1; i < targetTier; i++)
				{
					// The max amt for this tier is the following tiers min minus this tiers min.
					var tier = tiers[i];
					var max = tiers[i + 1].MinimumQuantity - tier.MinimumQuantity;

					if (!tier.Amount.TryGet(context, out uint tierAmount))
					{
						// Overrides min error.
						result.ErrorMessage = "Product is not currently available in your currency. If you believe this is a mistake, please get in touch.";
						result.ErrorCode = "product/not_available";
						return result;
					}

					var tierTotal = max * tierAmount;
					// A singular tier is expected to never be so large that it always overflows.
					// Adding it on however might do so.
					var prevTotal = totalCost;
					totalCost += tierTotal;

					// Overflow check:
					if (totalCost < prevTotal)
					{
						result.ErrorMessage = "The requested quantity is too large.";
						result.ErrorCode = "substantial_quantity";
						return result;
					}

				}

				// Handle any final excess.
				var finalTier = tiers[targetTier];
				var excess = quantity - (finalTier.MinimumQuantity - 1);

				if (!finalTier.Amount.TryGet(context, out uint finalTierAmount))
				{
					// Overrides min error.
					result.ErrorMessage = "Product is not currently available in your currency. If you believe this is a mistake, please get in touch.";
					result.ErrorCode = "product/not_available";
					return result;
				}

				var excessCost = excess * finalTierAmount;

				if (finalTierAmount != 0 && excessCost / finalTierAmount != excess)
				{
					result.ErrorMessage = "The requested quantity is too large.";
					result.ErrorCode = "substantial_quantity";
					return result;
				}

				var origTotal = totalCost;
				totalCost += excessCost;

				if (totalCost < origTotal)
				{
					result.ErrorMessage = "The requested quantity is too large.";
					result.ErrorCode = "substantial_quantity";
					return result;
				}
			}
			else
			{
				// Standard pricing strategy: use the price of the matched tier.
				var tier = tiers[targetTier];

				if (!tier.Amount.TryGet(context, out uint tierAmount))
				{
					result.ErrorMessage = "Product is not currently available in your currency.";
					result.ErrorCode = "product/not_available";
					return result;
				}

				totalCost = quantity * tierAmount;

				// Overflow check
				if (tierAmount != 0 && totalCost / tierAmount != quantity)
				{
					result.ErrorMessage = "The requested quantity is too large.";
					result.ErrorCode = "substantial_quantity";
					return result;
				}
			}

			var totalWithTax = totalCost;
			var totalWithoutTax = totalCost;

			if (taxCalculator != null && product.TaxExempt == 0)
			{
				totalWithTax = taxCalculator.Apply(totalCost);
			}else if(product.TaxExempt == 2)
			{
				totalWithTax = await taxCalculator.Apply(context, totalCost);
			}

			result.Amount = totalWithTax;
			result.AmountLessTax = totalWithoutTax;
			result.SubscriptionProducts = isSubscriptionProduct;

			return result;
		}

	}
    
}
