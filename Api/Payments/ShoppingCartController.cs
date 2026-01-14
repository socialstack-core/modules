using Api.Addresses;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Api.Payments
{
    /// <summary>Handles shoppingCart endpoints.</summary>
    [Route("v1/shoppingCart")]
	public partial class ShoppingCartController : AutoController<ShoppingCart>
    {

		/// <summary>
		/// Applies a coupon to the shopping cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="couponInfo"></param>
		/// <returns></returns>
		[HttpPost("apply_coupon")]
        public async ValueTask<ShoppingCart> ApplyCoupon(Context context, [FromBody] CartCoupon couponInfo)
        {
			return await (_service as ShoppingCartService)
				.ApplyCoupon(context,
					couponInfo.ShoppingCartId,
					couponInfo.AnonymousCartKey,
					couponInfo.Code
				);
		}

		/// <summary>
		/// Removes a coupon from the shopping cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="couponInfo"></param>
		/// <returns></returns>
		[HttpPost("remove_coupon")]
		public async ValueTask<ShoppingCart> RemoveCoupon(Context context, [FromBody] RemoveCoupon couponInfo)
		{
			return await (_service as ShoppingCartService)
				.RemoveCoupon(context,
					couponInfo.ShoppingCartId,
					couponInfo.AnonymousCartKey
				);
		}

		/// <summary>
		/// Loads a cart using anon key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cartId"></param>
		/// <param name="anonKey"></param>
		/// <returns></returns>
		[HttpGet("by-key/{cartId}/{anonKey}")]
		public async ValueTask<ShoppingCart> LoadAnon(Context context, [FromRoute] uint cartId, [FromRoute] string anonKey)
		{
			var cart = await (_service as ShoppingCartService)
				.Get(context,
					cartId, DataOptions.IgnorePermissions
				);

			if (cart == null || cart.AnonymousCartKey != anonKey || cart.CheckedOut)
			{
				return null;
			}

			return cart;
		}

		/// <summary>
		/// Adds or removes items from the specified cart. The contextual user must have access to the cart.
		/// If the cart ID is zero, or the current cart is invalid/ checked out, a new cart will be spawned and returned.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="itemChanges"></param>
		/// <returns></returns>
		[HttpPost("change_items")]
        public async ValueTask<ShoppingCart> ChangeItems(Context context, [FromBody] CartItemChanges itemChanges)
        {
            return await (_service as ShoppingCartService)
                .AddToCart(context,
					itemChanges.ShoppingCartId,
					itemChanges.AnonymousCartKey,
                    itemChanges.Items
                );
        }

		/// <summary>
		/// Checkout the cart. Fails if the cart is already checked out etc.
		/// </summary>
		/// <param name="httpContext"></param>
		/// <param name="context"></param>
		/// <param name="checkout"></param>
		/// <returns></returns>
		[HttpPost("checkout")]
		public async ValueTask<PurchaseAndAction?> Checkout(HttpContext httpContext, Context context, [FromBody] CheckoutInfo checkout)
		{
			var cart = await (_service as ShoppingCartService).Get(context, checkout.ShoppingCartId, DataOptions.IgnorePermissions);

			if (cart == null || cart.CheckedOut || checkout.AnonymousCartKey != cart.AnonymousCartKey)
			{
				return null;
			}

			checkout.IpAddress = RequestHelper.GetClientIp(httpContext);
			
			if (!string.IsNullOrWhiteSpace(cart.Reference))
			{
				checkout.Reference = cart.Reference;
			}

			return await (_service as ShoppingCartService)
				.Checkout(
					context,
					cart,
					checkout
				);
		}

	}

	/// <summary>
	/// Checking out a cart.
	/// </summary>
	public partial struct CheckoutInfo
	{
		/// <summary>
		/// The cart ID.
		/// </summary>
		public uint ShoppingCartId;

		/// <summary>
		/// Anon cart key (albeit users are expected to be logged in, but they still won't own the cart).
		/// </summary>
		public string AnonymousCartKey;

		/// <summary>
		/// The key used for the delivery address
		/// </summary>
		public string DeliveryAddressKey;

		/// <summary>
		/// The key used to find the billing address
		/// </summary>
		public string BillingAddressKey;

		/// <summary>
		/// The delivery address
		/// </summary>
		[JsonIgnore]
		public Address DeliveryAddress;

		/// <summary>
		/// The billing address
		/// </summary>
		[JsonIgnore]
		public Address BillingAddress;
		
		/// <summary>
		/// Delivery option if necessary.
		/// </summary>
		public uint DeliveryOptionId;

		/// <summary>
		/// The ip addess of the client
		/// </summary>
		public string IpAddress;

		/// <summary>
		/// The unique reference created for this purchase (passed from cart)
		/// </summary>
		public string Reference;

		/// <summary>
		/// If using a saved payment method, the ID of it or the details for a one off payment use.
		/// This can be null if it's a free or buy now pay later order. 
		/// Buy now pay later orders must be available to the user.
		/// </summary>
		public JToken PaymentMethod;

		/// <summary>
		/// Get the billing address (does not update the struct itself)
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<Address> GetBillingAddress(Context context)
		{
			if(BillingAddress == null && !string.IsNullOrEmpty(BillingAddressKey))
			{
				return await Services.Get<AddressService>().Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(BillingAddressKey).First(context);
			}
			return BillingAddress;
		}

		/// <summary>
		/// Get the delivery address (does not update the struct itself)
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<Address> GetDeliveryAddress(Context context)
		{
			if(DeliveryAddress == null && !string.IsNullOrEmpty(DeliveryAddressKey))
			{
				return await Services.Get<AddressService>().Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(DeliveryAddressKey).First(context);
			}
			return DeliveryAddress;
		}
	}

	/// <summary>
	/// Represents quantity or quantity change in a cart. Only one or the other - not both.
	/// </summary>
	public struct CartQuantity
    {
		/// <summary>
		/// The delta quantity. Use this or Total.
		/// </summary>
		public int? Delta;

		/// <summary>
		/// The total desired quantity. Use this or Delta.
		/// </summary>
		public uint? Total;
	}

    /// <summary>
    /// A set of items to change within a cart.
    /// </summary>
    public struct CartItemChanges
    {
		/// <summary>
		/// The shopping cart, which can be zero if you need a new cart object.
		/// </summary>
		public uint ShoppingCartId;

		/// <summary>
		/// Enables anon cart updates. Can be null if you need a new one.
		/// </summary>
		public string AnonymousCartKey;

		/// <summary>
		/// Items to add/ remove.
		/// </summary>
		public List<CartItemChange> Items;
    }
    
    /// <summary>
    /// Changing the coupon on a cart.
    /// </summary>
    public struct RemoveCoupon
	{
		/// <summary>
		/// Enables anon cart updates.
		/// </summary>
		public string AnonymousCartKey;
		
		/// <summary>
		/// The shopping cart
		/// </summary>
		public uint ShoppingCartId;
	}
	
    /// <summary>
    /// Changing the coupon on a cart.
    /// </summary>
    public struct CartCoupon
	{
		/// <summary>
		/// The coupon to apply. If this is null, nothing happens.
		/// </summary>
		public string Code;

		/// <summary>
		/// Enables anon cart updates.
		/// </summary>
		public string AnonymousCartKey;

		/// <summary>
		/// The shopping cart
		/// </summary>
		public uint ShoppingCartId;
	}

	/// <summary>
	/// Changing (usually an addition) the quantity of an item in a cart.
	/// </summary>
	public struct CartItemChange
    {   
        /// <summary>
        /// The product to change.
        /// </summary>
        public uint ProductId;

		/// <summary>
		/// The delta quantity. Use this or Quantity.
		/// </summary>
		public int? DeltaQuantity;

        /// <summary>
        /// The total desired quantity. Use this or DeltaQuantity.
        /// </summary>
        public uint? Quantity;
    }
}