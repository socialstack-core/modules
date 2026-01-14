using Amazon.S3.Model;
using Api.Addresses;
using Api.Contexts;
using Api.Eventing;
using Api.GuestUsers;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// Handles shoppingCart endpoints.
/// </summary>
public partial class ShoppingCartController
{

	/// <summary>
	/// Update a cart adding the guest details
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cartId"></param>
	/// <param name="anonKey"></param>
	/// <param name="guestDetails"></param>
	/// <returns></returns>
	[HttpPost("/updateGuest/{cartId}/{anonKey}")]
	public async ValueTask<ShoppingCart> UpdateGuest(Context context, [FromRoute] uint cartId, [FromRoute] string anonKey, [FromBody] GuestDetails guestDetails)
	{
		return await (_service as ShoppingCartService).UpdateGuestDetails(context, cartId, anonKey, guestDetails);
	}


	/// <summary>
	/// Submits the cart. May go to a manager first or become an order. Fails if the cart is already checked out etc.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="context"></param>
	/// <param name="checkoutInfo"></param>
	/// <returns></returns>
	[HttpPost("checkout-guest-cart")]
	public async ValueTask<PurchaseAndAction?> CheckoutGuestCart(HttpContext httpContext, Context context, [FromBody] CheckoutInfo checkoutInfo)
	{
		if (context.UserId != 0 || context.RoleId != 6)
		{
			throw new PublicException("Account users cannot checkout as a guest", "Purchase/Invalid_User");
		}

		var carts = (_service as ShoppingCartService);

		var cart = await carts.Get(context, checkoutInfo.ShoppingCartId, DataOptions.IgnorePermissions);

		if (cart == null || cart.CheckedOut || cart.AnonymousCartKey != checkoutInfo.AnonymousCartKey)
		{
			return null;
		}

		if (cart.GuestDetails == null)
		{
			return null;
		}

		checkoutInfo.IpAddress = RequestHelper.GetClientIp(httpContext);

		if (!string.IsNullOrWhiteSpace(cart.Reference))
		{
			checkoutInfo.Reference = cart.Reference;
		}

		// Grab our addresses. We are using the AnonKey values to ensure that the addresses we are using were not tampered with and the user has permission to place an order for them.
		var _addressService = Services.Get<AddressService>();
		var deliveryAddress = await _addressService.Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(checkoutInfo.DeliveryAddressKey).First(context);

		if (deliveryAddress == null)
		{
			throw new PublicException("Unable to find the delivery address, please contact us for assistance", "address_notfound");
		}
		
		Address billingAddress = deliveryAddress;
		if (!string.IsNullOrEmpty(checkoutInfo.BillingAddressKey) && checkoutInfo.BillingAddressKey != checkoutInfo.DeliveryAddressKey)
		{
			billingAddress = await _addressService.Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(checkoutInfo.BillingAddressKey).First(context);
		}

		// add the guest user 
		var guestUser = new GuestUser()
		{
			FirstName = cart.GuestDetails.FirstName,
			LastName = cart.GuestDetails.LastName,
			Email = cart.GuestDetails.Email,
			DeliveryAddressId = deliveryAddress.Id,
			BillingAddressId = billingAddress != null ? billingAddress.Id : deliveryAddress.Id
		};

		guestUser = await Events.Purchase.BeforeCreateGuest.Dispatch(context, guestUser, cart.GuestDetails, cart);

		var _guestUserService = Services.Get<GuestUserService>();

		guestUser  = await _guestUserService.Create(context, guestUser, DataOptions.IgnorePermissions);

		if (guestUser == null)
		{
			throw new PublicException("Unable to process your request, please contact us for assistance", "guest_notcreated");
		}
		
		checkoutInfo.GuestUserId = guestUser.Id;

		// finally checkout the order for the guest 
		var info = await carts.Checkout(context, cart, checkoutInfo);

		await Events.ShoppingCart.AfterGuestCheckout.Dispatch(context, cart, info, guestUser);

		return info;
	}
}



