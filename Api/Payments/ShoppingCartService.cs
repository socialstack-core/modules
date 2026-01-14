using Api.Addresses;
using Api.Contexts;
using Api.Counters;
using Api.Eventing;
using Api.PasswordResetRequests;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Handles shoppingCarts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ShoppingCartService : AutoService<ShoppingCart>
	{
		private ProductQuantityService _productQuantities;
		private PaymentMethodService _paymentMethods;
		private AddressService _addressService;
		private PurchaseService _purchases;
		private ProductService _products;
		private CouponService _coupons;
		private PriceService _prices;
		private CounterService _counters;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ShoppingCartService(ProductQuantityService productQuantities, PurchaseService purchases,
			ProductService products, CouponService coupons, AddressService addressService, PriceService prices,
			PaymentMethodService paymentMethods, CounterService counters) : base(Events.ShoppingCart)
		{
			_productQuantities = productQuantities;
			_addressService = addressService;
			_paymentMethods = paymentMethods;
			_purchases = purchases;
			_products = products;
			_coupons = coupons;
			_prices = prices;
			_counters = counters;

			Events.ShoppingCart.BeforeSettable.AddEventListener((Context context, JsonField<ShoppingCart, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<ShoppingCart, uint>>(field);
				}

				if (field.Name == "CouponId" && !field.ForRole.CanViewAdmin)
				{
					// This field isn't settable by non-admins.
					field = null;
				}

				return new ValueTask<JsonField<ShoppingCart, uint>>(field);
			});

			Events.Purchase.BeforeUpdate.AddEventListener(async (Context context, Purchase toUpdate, Purchase original) =>
			{
				if (toUpdate == null)
				{
					return null;
				}

				// If the purchase is transitioning and is for a shopping cart, then we will potentially update the cart itself.
				// This is ultimately such that the users cart empties out.
				if (toUpdate.ContentType == "ShoppingCart" &&
					toUpdate.Status != original.Status &&
					toUpdate.Status >= 200 && toUpdate.Status <= 299)
				{
					var cart = await Get(context, toUpdate.ContentId, DataOptions.IgnorePermissions);

					if (cart != null && !cart.CheckedOut)
					{
						await Update(context, cart, (Context ctx, ShoppingCart cartToUpdate, ShoppingCart origCart) =>
						{
							cartToUpdate.CheckedOut = true;
						}, DataOptions.IgnorePermissions);
					}
				}

				return toUpdate;
			});

			Events.ShoppingCart.BeforeCreate.AddEventListener(async (Context context, ShoppingCart cart) =>
			{
				if (cart == null)
				{
					return cart;
				}

				// Generate an anon key for update and retrieval of the cart whilst not logged in:
				cart.AnonymousCartKey = RandomToken.Generate(16);

				// Generate the unique order number/reference (will be passed onto purchase)
				cart.Reference = await _counters.GetCounter("PurchaseId", "WEB");

				return cart;
			});

			Events.Purchase.Checkout.AddEventListener(async (Context context, Purchase purchase, CheckoutInfo checkoutInfo) =>
			{
				if (purchase == null)
				{
					return purchase;
				}

				var cart = await Get(context, checkoutInfo.ShoppingCartId, DataOptions.IgnorePermissions);
				if (cart == null)
				{
					return purchase;
				}

				// Copy over the saved snapshot details which may have been saved if the cart had items removed etc
				var originalRequests = await GetOriginalProductQuantities(context, cart);
				if (originalRequests == null || originalRequests.Count == 0)
				{
					return purchase;
				}

				List<ulong> ids = new List<ulong>();
				foreach (var entry in originalRequests)
				{
					var purchaseQuantity = new ProductQuantity()
					{
						ProductId = entry.ProductId,
						Quantity = entry.Quantity,
						OrderedCurrencyCode = entry.OrderedCurrencyCode,
						OrderedTotal = entry.OrderedTotal,
						OrderedTotalLessTax = entry.OrderedTotalLessTax
					};

					var result = await _productQuantities.Create(context, purchaseQuantity, DataOptions.IgnorePermissions);

					if (result == null)
					{
						continue;
					}

					ids.Add(result.Id);
				}

				// Add to mapping (unsaved):
				if (ids.Count > 0) { 
					purchase.Mappings.Set("RequestedProductQuantities", ids);
				}

				return purchase;
			});
		}

		/// <summary>
		/// Gets the pricing info for a given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="billingAddressKey"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantityPricing> GetPricingInfo(Context context, ShoppingCart cart, string billingAddressKey)
		{
			// Billing addr sets the final tax jurisdiction if one is set:
			var billingAddress = string.IsNullOrEmpty(billingAddressKey) ? null :
				await _addressService.Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(billingAddressKey).First(context);

			return await GetPricingInfo(context, cart, billingAddress);
		}

		/// <summary>
		/// Gets the pricing info for a given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="billingAddressId"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantityPricing> GetPricingInfo(Context context, ShoppingCart cart, uint billingAddressId)
		{
			// Billing addr sets the final tax jurisdiction if one is set:
			var billingAddress = billingAddressId == 0 ? null :
					await _addressService.Get(context, billingAddressId, DataOptions.IgnorePermissions);

			return await GetPricingInfo(context, cart, billingAddress);
		}

		/// <summary>
		/// Gets the pricing info for a given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="billingAddress"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantityPricing> GetPricingInfo(Context context, ShoppingCart cart, Address billingAddress)
		{
			// Get the tax jurisdiction:
			var taxJurisdiction = billingAddress == null ? cart.TaxJurisdiction :
				await _addressService.GetTaxJurisdiction(context, billingAddress);

			// Items are copied from the shopping cart to the purchase.
			// This prevents any risk of someone manipulating their cart during the fulfilment.
			var inCart = await GetProductQuantities(context, cart);

			// Calculate the total:
			var pricingInfo = await _productQuantities.GetPricing(context, inCart, taxJurisdiction, cart.CouponId);

			return pricingInfo;
		}

		/// <summary>
		/// Checkout the cart using the given payment method. Intended to be called by actual user context.
		/// Returns a Purchase object which contains a clone of the objects in the cart.
		/// Will throw publicExceptions if the payment failed.
		/// You should however check the purchase.Status for immediate failures as well.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="checkoutInfo">
		/// General checkout options such as delivery address (if one is necessary) etc.
		/// </param>
		public async ValueTask<PurchaseAndAction> Checkout(Context context, ShoppingCart cart, CheckoutInfo checkoutInfo)
		{
			// Create a purchase (uses the user's locale from context):

			var paymentMethodInfo = checkoutInfo.PaymentMethod;

			var pricingInfo = await GetPricingInfo(context, cart, checkoutInfo.BillingAddressKey);

			var paymentMethod = await _paymentMethods.GetFromJson(context, paymentMethodInfo, pricingInfo.HasSubscriptionProducts);

			var purchaseResult = await _purchases.CreateAndExecute(
				context,
				pricingInfo,
				"ShoppingCart",
				cart.Id,
				paymentMethod,

				// *severe tax liability danger*
				false, // Do not change the tax exclusion state unless you really, really, really know what you are doing.
					   // Unless the customer is foreign, you are probably making a mistake.
					   // B2B in the UK is *NOT* tax exempt!
					   // Yes, it is correct to display someone a VAT
					   // free price and then charge them VAT on top of it anyway!


				checkoutInfo
			);
			// The cart itself will have its status updated when the purchase transitions
			// (i.e. after any action has concluded).

			return purchaseResult;
		}

		/// <summary>
		/// Calculates the prices for the given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantityPricing> GetContentPrices(Context context, ShoppingCart cart)
		{
			// Get the pq's:
			var productQuants = await GetProductQuantities(context, cart);

			return await _productQuantities.GetPricing(context, productQuants, cart.TaxJurisdiction, cart.CouponId);
		}

		/// <summary>
		/// Gets the products in the given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProductQuantities(Context context, ShoppingCart cart)
		{
			return await _productQuantities
				.ListBySource(context, cart, "ProductQuantities", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Gets the requested products in the given cart. Null if the cart has not been modified.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetOriginalProductQuantities(Context context, ShoppingCart cart)
		{
			return await _productQuantities
				.ListBySource(context, cart, "RequestedProductQuantities", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Removes a coupon from the cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cartId"></param>
		/// <param name="anonKey"></param>
		/// <returns></returns>
		public async ValueTask<ShoppingCart> RemoveCoupon(Context context, uint cartId, string anonKey)
		{
			var cart = await Get(context, cartId, DataOptions.IgnorePermissions);

			if (cart.AnonymousCartKey != anonKey || cart.CheckedOut)
			{
				// Nope!
				return null;
			}

			return await Update(context, cart, (Context ctx, ShoppingCart toUpdate, ShoppingCart original) =>
			{
				toUpdate.CouponId = 0;
			}, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Applies a coupon to the cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cartId"></param>
		/// <param name="anonKey"></param>
		/// <param name="couponCode"></param>
		/// <returns></returns>
		public async ValueTask<ShoppingCart> ApplyCoupon(Context context, uint cartId, string anonKey, string couponCode)
		{
			if (string.IsNullOrEmpty(couponCode))
			{
				return null;
			}

			var coupon = await _coupons.Where("Token=?", DataOptions.IgnorePermissions).Bind(couponCode).First(context);

			if (coupon == null)
			{
				throw new PublicException("That coupon code does not exist.", "coupon/not_found");
			}

			if (coupon.Disabled || (coupon.ExpiryDateUtc.HasValue && coupon.ExpiryDateUtc.Value < System.DateTime.UtcNow))
			{
				// NB: If max number of people is reached, it is marked as disabled.
				throw new PublicException("Unfortunately the provided coupon has expired.", "coupon/expired");
			}

			var cart = await Get(context, cartId, DataOptions.IgnorePermissions);

			if (cart.AnonymousCartKey != anonKey || cart.CheckedOut)
			{
				// Nope!
				return null;
			}

			return await Update(context, cartId, (Context ctx, ShoppingCart toUpdate, ShoppingCart original) =>
			{
				toUpdate.CouponId = coupon.Id;
			}, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Adds the given product to the cart. Will spawn new carts when necessary.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cartId"></param>
		/// <param name="anonKey"></param>
		/// <param name="itemChanges"></param>
		/// <returns></returns>
		public async ValueTask<ShoppingCart> AddToCart(Context context, uint cartId, string anonKey, List<CartItemChange> itemChanges)
		{
			ShoppingCart cart = null;

			if (cartId != 0)
			{
				// Get the cart:
				cart = await Get(context, cartId, DataOptions.IgnorePermissions);

				if (cart == null || cart.AnonymousCartKey != anonKey || cart.CheckedOut)
				{
					cart = null;
				}
			}

			if (cart == null)
			{
				// Spawn a new cart.
				var locale = await context.GetLocale();

				cart = await Create(context, new ShoppingCart()
				{
					TaxJurisdiction = locale.DefaultTaxJurisdiction
				}, DataOptions.IgnorePermissions);
			}

			// Run change event:
			cart = await Events.ShoppingCart.ChangeItems.Dispatch(context, cart, itemChanges);

			// Carts aren't cached but we'll copy it anyway just in case a site does choose to cache them.
			// This copy allows us to safely manipulate the mapping set outside of the Update callbacks.
			var quantities = cart.Mappings.GetCopy("ProductQuantities");
			if (quantities == null)
			{
				quantities = new List<ulong>();
			}

			foreach (var change in itemChanges)
			{
				// Get the product:
				var product = await _products.Get(context, change.ProductId);

				if (product == null)
				{
					throw new PublicException("Product '" + change.ProductId + "' was not found", "product/not_found");
				}

				await _productQuantities.ChangeItems(context, quantities, product, new CartQuantity()
				{
					Total = change.Quantity,
					Delta = change.DeltaQuantity
				});
			}

			cart = await Update(context, cart, (Context ctx, ShoppingCart toUpdate, ShoppingCart orig) =>
			{

				// Update the carts editedUtc such that other checkout
				// features are aware that the cart has changed.
				toUpdate.EditedUtc = DateTime.UtcNow;

				// Set the PQ's:
				toUpdate.Mappings.Set("ProductQuantities", quantities);

			}, DataOptions.IgnorePermissions);


			return cart;
		}

		/*
		/// <summary>
		/// Adds the given product to the cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="productId"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantity> AddToCart(Context context, ShoppingCart cart, uint productId, CartQuantity quantity)
		{
			// Get the product:
			var product = await _products.Get(context, productId, DataOptions.IgnorePermissions);

			if (product == null)
			{
				throw new PublicException("Product was not found", "product/not_found");
			}

			return await AddToCart(context, cart, productId, quantity);
		}
		*/

	}

}
