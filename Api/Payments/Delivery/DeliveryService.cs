using AngleSharp.Html.Dom.Events;
using Api.Addresses;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Handles delivery times.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DeliveryService : AutoService<Delivery>
    {
		private DeliveryOptionService _options;
		private AddressService _addresses;
		private ProductQuantityService _productQuantities;
		private ShoppingCartService _carts;
		private PriceService _prices;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DeliveryService(DeliveryOptionService options, AddressService addresses, 
			ProductQuantityService productQuantities, PriceService prices) : base(Events.Delivery)
        {
			_prices = prices;
			_options = options;
			_addresses = addresses;
			_productQuantities = productQuantities;
		}

		/// <summary>
		/// Set up the delivery objects on a purchase being created based on the given selected delivery estimate.
		/// You must save the purchase after this as the deliveries set is applied to the given purchase but not saved.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="allItems"></param>
		/// <param name="estimate"></param>
		/// <returns></returns>
		public async ValueTask<List<Delivery>> SetupDeliveries(Context context, Purchase purchase, List<LineItem> allItems, DeliveryEstimate estimate)
		{
			if (estimate == null)
			{
				// E.g. digital goods
				return null;
			}

			var delivs = estimate.Deliveries;

			if (delivs == null || delivs.Length ==0)
			{
				// Same reason - digi goods.
				return null;
			}

			var deliveryIds = new List<ulong>();
			var deliveries = new List<Delivery>();
			var isFirst = true;

			foreach (var deliveryInfo in delivs)
			{
				var unsavedDelivery = new Delivery()
				{
					DeliveryNotes = deliveryInfo.DeliveryNotes,
					DeliveryName = deliveryInfo.DeliveryName,
					TimeWindowLength = deliveryInfo.TimeWindowLength,
					ExpectedSlotUtc = deliveryInfo.SlotStartUtc
				};

				if (isFirst)
				{
					isFirst = false;
					unsavedDelivery.DeliveryCost = estimate.Price;
					unsavedDelivery.DeliveryCostLessTax = estimate.PriceLessTax;
				}

				var items = new List<ulong>();
				var itemsWithProductsIfLoaded = new List<DeliveryItem>();
				uint totalPrice = 0;
				uint totalPriceLessTax = 0;

				if (deliveryInfo.Products == null || deliveryInfo.Products.Count == 0)
				{
					if (delivs.Length > 1)
					{
						// Must set deliveryInfo.Products if the delivery is split.
						throw new PublicException("Delivery is split but does not specify the products in each option", "delivery/split-invalid");
					}

					// Otherwise, it's everything in the order. Can share objects here - order lines are readonly.
					if (allItems == null)
					{
						throw new PublicException("No items to deliver despite delivery specified", "delivery/invalid");
					}

					foreach (var item in allItems)
					{
						if (item.ProductQuantityId == 0)
						{
							throw new PublicException("Originating product quantity required (internal)", "delivery/missing-pq");
						}

						totalPrice += (uint)item.Total;
						totalPriceLessTax += (uint)item.TotalLessTax;

						items.Add(item.ProductQuantityId);
						itemsWithProductsIfLoaded.Add(new DeliveryItem(item.ProductQuantity, item.Product));
					}
				}
				else
				{
					foreach (var item in deliveryInfo.Products)
					{
						totalPrice += (uint)item.Total;
						totalPriceLessTax += (uint)item.TotalLessTax;

						var pq = await _productQuantities.Create(context, new ProductQuantity() {
							ProductId = item.ProductId,
							Quantity = item.Quantity,
						}, DataOptions.IgnorePermissions);

						if (pq != null)
						{
							items.Add(pq.Id);
							itemsWithProductsIfLoaded.Add(new DeliveryItem(pq, null));
						}
					}
				}

				// Set totals:
				unsavedDelivery.TotalCost = totalPrice + unsavedDelivery.DeliveryCost;
				unsavedDelivery.TotalCostLessTax = totalPriceLessTax + unsavedDelivery.DeliveryCostLessTax;

				// Handle any custom delivery fields:
				await Events.Delivery.Setup.Dispatch(context, unsavedDelivery, purchase);

				unsavedDelivery.Mappings.Set("ProductQuantities", items);
				unsavedDelivery.Items = itemsWithProductsIfLoaded;
				var delivery = await Create(context, unsavedDelivery, DataOptions.IgnorePermissions);

				if (delivery == null)
				{
					continue;
				}

				deliveryIds.Add(delivery.Id);
				deliveries.Add(delivery);
			}

			purchase.Mappings.Set("deliveries", deliveryIds);
			return deliveries;
		}

		/// <summary>
		/// Gets the deliveries on a purchase.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<List<Delivery>> GetDeliveries(Context context, Purchase purchase)
		{
			return await ListBySource(context, purchase, "Deliveries", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Gets the products in the given delivery.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="delivery"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProductQuantities(Context context, Delivery delivery)
		{
			return await _productQuantities
				.ListBySource(context, delivery, "ProductQuantities", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Collects a potentially cached set of delivery estimates for the given shopping cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async ValueTask<List<DeliveryOption>> EstimateDelivery(Context context, ShoppingCart cart, CartEstimation options)
		{
			Address address;

			if (string.IsNullOrEmpty(options.DeliveryAddressKey))
			{
				// Not specified by the request - use the one on the cart.
				if (cart.DeliveryAddressId == 0)
				{
					throw new PublicException("A target delivery address has not been set.", "delivery/no_address");
				}

				address = await _addresses.Get(context, cart.DeliveryAddressId, DataOptions.IgnorePermissions);
			}
			else
			{
				address = await _addresses.Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(options.DeliveryAddressKey).First(context);
			}

			if (address == null)
			{
				throw new PublicException("A target delivery address has not been set.", "delivery/no_address");
			}

			return await EstimateDelivery(context, cart, address);
		}

		/// <summary>
		/// Gets the parsed delivery estimate for the given delivery option ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="deliveryOptionId"></param>
		/// <returns></returns>
		public async ValueTask<DeliveryEstimate> GetEstimate(Context context, uint deliveryOptionId)
		{
			// Load the option:
			var option = await _options.Get(context, deliveryOptionId, DataOptions.IgnorePermissions);

			return await GetEstimate(context, option);
		}

		/// <summary>
		/// Gets the parsed delivery estimate for the given delivery option ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="deliveryOption"></param>
		/// <returns></returns>
		public async ValueTask<DeliveryEstimate> GetEstimate(Context context, DeliveryOption deliveryOption)
		{
			if (deliveryOption == null)
			{
				return null;
			}

			return Newtonsoft.Json.JsonConvert.DeserializeObject<DeliveryEstimate>(deliveryOption.InformationJson);
		}

		/// <summary>
		/// Collects a potentially cached set of delivery estimates for the given shopping cart and target address.
		/// </summary>
		private async ValueTask<List<DeliveryOption>> EstimateDelivery(Context context, ShoppingCart cart, Address targetAddress)
		{
			// Get the current estimate set for this cart:
			var currentOpts = await _options
				.Where("ShoppingCartId=?", DataOptions.IgnorePermissions)
				.Bind(cart.Id).ListAll(context);

			if (cart.CheckedOut)
			{
				// Readonly
				return currentOpts;
			}

			var skipStaleCheck = false;
			if(cart.DeliveryOptionId != 0)
			{
				var tomorrowLocal = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

				//A delivery option has been selected but not checked out.
				//We must check if the current options are still valid.
				var newOpts = new List<DeliveryOption>();
				for(int i = 0; i < currentOpts.Count; i++)
				{
					var option = currentOpts[i];
					DeliveryEstimate info = null;

					if(!string.IsNullOrEmpty(option.InformationJson))
					{
						info = JsonConvert.DeserializeObject<DeliveryEstimate>(option.InformationJson);
					}

					if(info == null || (info.RequestedDeliveryDate < tomorrowLocal && option.Id != cart.DeliveryOptionId))
					{
						//The option is invalid or out of date, so delete it
						await _options.Delete(context, option, DataOptions.IgnorePermissions);
					}
					else
					{
						newOpts.Add(option);
					}
				}
				currentOpts = newOpts;

				if(currentOpts.Count > 0)
				{
					skipStaleCheck = true;
				}
			}

			// If the cart itself has not changed since the last estimate
			// then return the prior set.
			var lastModified = cart.EditedUtc;
			var anHourAgo = DateTime.UtcNow.AddHours(-1);

			skipStaleCheck = skipStaleCheck ? skipStaleCheck : cart.DeliveryAddressId == targetAddress.Id;

			if (!skipStaleCheck && currentOpts != null && currentOpts.Count > 0)
			{
				var stale = false;

				foreach (var item in currentOpts)
				{
					if (item.CreatedUtc < lastModified || item.CreatedUtc < anHourAgo || item.AddressId != targetAddress.Id)
					{
						// It's older than the cart is, or more than an hour old. Regen them all.
						stale = true;
						break;
					}
				}

				if (stale)
				{
					// Cull them all.
					foreach (var item in currentOpts)
					{
						await _options.Delete(context, item, DataOptions.IgnorePermissions);
					}
				}
				else
				{
					// As-is
					return currentOpts;
				}
			}

			if (_carts == null)
			{
				_carts = Services.Get<ShoppingCartService>();
			}

			var inCart = await _carts.GetProductQuantities(context, cart);
			var taxJurisdiction = await _addresses.GetTaxJurisdiction(context, targetAddress);

			// Calculate the total values of what's in the cart - this impacts tax due on the delivery itself:
			var pricingInfo = await _productQuantities.GetPricing(context, inCart, taxJurisdiction, cart.CouponId);

			var deliveryPricingDetail = _productQuantities.GetDeliveryDetail(pricingInfo);

			if (deliveryPricingDetail == null)
			{
				// Not physically delivered or deliverable.
				return null;
			}

			// Get tax calc:
			var taxCalc = await _prices.GetTaxCalculator(context, taxJurisdiction);

			// Collect the estimates now. This is very site specific so you'll need to create a handler for this event.
			var estimate = await Events.DeliveryOption.Estimate.Dispatch(context, new DeliveryEstimates()
			{
				Cart = cart,
				DeliveryPricing = deliveryPricingDetail.Value,
				TaxJurisdiction = taxJurisdiction,
				Pricing = pricingInfo,
				TaxCalculator = taxCalc,
				Target = targetAddress,
			});

			if (estimate.Options == null)
			{
				// Not physically delivered or deliverable.
				return null;
			}

			//Grab the currentOpts again as they may have been updated during the dispatch
			currentOpts = await _options
				.Where("ShoppingCartId=?", DataOptions.IgnorePermissions)
				.Bind(cart.Id).ListAll(context);

			// Store them:
			var opts = new List<DeliveryOption>();

			foreach (var item in estimate.Options)
			{
				var estimateJson = Newtonsoft.Json.JsonConvert.SerializeObject(item, jsonSettings);

				//Check if our estimateJson matches any existing options
				var alreadyAdded = false;
				if(currentOpts != null && currentOpts.Count > 0)
				{
					foreach(var option in currentOpts)
					{
						if(option.AddressId == targetAddress.Id && estimateJson == option.InformationJson)
						{
							opts.Add(option);
							alreadyAdded = true;
							break;
						}
					}
				}

				if(alreadyAdded)
				{
					continue;
				}

				var opt = await _options.Create(context, new DeliveryOption()
				{
					AddressId = targetAddress.Id,
					ShoppingCartId = cart.Id,
					InformationJson = estimateJson
				}, DataOptions.IgnorePermissions);

				opts.Add(opt);
			}

			return opts;
		}

		/// <summary>
		/// Create a new batch of estimates for a cart and select the most appropriate. This is used for split orders so it is expected that a delivery option has already been selected for the original order for us to work from
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="deliveryAddress"></param>
		/// <param name="deliveryInformation"></param>
		/// <param name="update"></param>
		/// <returns></returns>
		public async ValueTask<DeliveryOption> UpdateCartDeliveryOption(Context context, ShoppingCart cart, Address deliveryAddress, DeliveryEstimate deliveryInformation, bool update = true)
		{
			var newDeliveryOptions = await EstimateDelivery(context, cart, deliveryAddress);

			//Find the delivery option that matches the original selection (if any)
			DeliveryOption newDeliveryOption = null;
			if (newDeliveryOptions != null && deliveryInformation != null)
			{
				DeliveryOption earliestOption = null;
				DeliveryEstimate earliestEstimate = null;
				foreach (var option in newDeliveryOptions)
				{
					DeliveryEstimate currentDeliveryInformation = null;
					if (!string.IsNullOrWhiteSpace(option.InformationJson))
					{
						try
						{
							currentDeliveryInformation = JsonConvert.DeserializeObject<DeliveryEstimate>(
								option.InformationJson
							);
						}
						catch (Exception e)
						{
							// This should never happen as estimates have to happen to get this far
							Log.Error(LogTag, e, $"Failed to deserialize delivery information for cart {cart.Id} with delivery option {option.Id}");
						}
					}

					if (currentDeliveryInformation != null && currentDeliveryInformation.DeliveryCode == deliveryInformation.DeliveryCode)
					{
						if(currentDeliveryInformation.RequestedDeliveryDate == deliveryInformation.RequestedDeliveryDate)
						{
							newDeliveryOption = option;
							break;
						}else if(earliestEstimate == null || earliestEstimate.RequestedDeliveryDate > currentDeliveryInformation.RequestedDeliveryDate)
						{
							earliestOption = option;
							earliestEstimate = currentDeliveryInformation;
						}
					}
				}

				if(newDeliveryOption == null && earliestOption != null)
				{
					//We cannot find a match so assume that the date has somehow become unavailable. Use the earliest available option with a matching code
					newDeliveryOption = earliestOption;
				}
			}

			//Update the cart with the new delivery option and price
			if (update && newDeliveryOption != null)
			{
				await _carts.Update(context, cart, (Context ctx, ShoppingCart toUpdate, ShoppingCart original) =>
				{
					toUpdate.DeliveryOptionId = newDeliveryOption.Id;
				}, DataOptions.IgnorePermissions);
			}

			return newDeliveryOption;
		}

		/// <summary>
		/// Json serialization settings for delivery options
		/// </summary>
		public readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

	}

	/// <summary>
	/// A delivery with the items in it already loaded.
	/// </summary>
	public class DeliveryAndItems
	{
		/// <summary>
		/// The delivery itself.
		/// </summary>
		public Delivery Delivery;
		/// <summary>
		/// The items in the delivery.
		/// </summary>
		public List<DeliveryItem> Items;
	}

	/// <summary>
	/// An item in a delivery.
	/// </summary>
	public struct DeliveryItem
	{
		/// <summary>
		/// The underlying PQ. Always set.
		/// </summary>
		public ProductQuantity ProductQuantity;

		/// <summary>
		/// The product ID.
		/// </summary>
		public uint ProductId => ProductQuantity.ProductId;

		/// <summary>
		/// The quantity.
		/// </summary>
		public ulong Quantity => ProductQuantity.Quantity;

		/// <summary>
		/// The underlying product (may be null).
		/// </summary>
		public Product Product;


		/// <summary>
		/// Creates a new delivery item with optional product info.
		/// </summary>
		/// <param name="productQuantity"></param>
		/// <param name="product"></param>
		public DeliveryItem(ProductQuantity productQuantity, Product product)
		{
			ProductQuantity = productQuantity;
			Product = product;
		}
	}

}
