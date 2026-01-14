using Api.Addresses;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>Handles delivery option endpoints.</summary>
[Route("v1/deliveryoption")]
public partial class DeliveryOptionController : AutoController<DeliveryOption>
{
	private DeliveryService _deliveries;
	private DeliveryOptionService _options;
	private ShoppingCartService _carts;

	/// <summary>
	/// Instanced automatically (singleton).
	/// </summary>
	/// <param name="deliveries"></param>
	/// <param name="carts"></param>
	/// <param name="options"></param>
	public DeliveryOptionController(DeliveryService deliveries, ShoppingCartService carts, DeliveryOptionService options)
	{
		_deliveries = deliveries;
		_carts = carts;
		_options = options;
	}

	/// <summary>
	/// Estimate the given shopping cart. The contextual user must be able to load the cart of course!
	/// If estimates are already generated, this will return the existing set.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="shoppingCartId"></param>
	/// <param name="anonKey"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	[HttpPost("estimate/cart/{shoppingCartId}/by-key/{anonKey}")]
	public async ValueTask<ContentStream<DeliveryOption, uint>?> Estimate(Context context, [FromRoute] uint shoppingCartId, [FromRoute] string anonKey, [FromBody] CartEstimation options)
	{
		var cart = await _carts.Get(context, shoppingCartId, DataOptions.IgnorePermissions);

		if (cart == null || cart.AnonymousCartKey != anonKey)
		{
			return null;
		}

		var estimates = await _deliveries.EstimateDelivery(context, cart, options);
		return new ContentStream<DeliveryOption, uint>(estimates, _options);
	}

}

/// <summary>
/// Used when estimating a cart.
/// </summary>
public struct CartEstimation
{
	/// <summary>
	/// Address override.
	/// </summary>
	public string DeliveryAddressKey;
}