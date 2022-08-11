using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>Handles purchase endpoints.</summary>
	[Route("v1/purchase")]
	public partial class PurchaseController : AutoController<Purchase>
	{

		/// <summary>
		/// POST /v1/purchase/submit
		/// Creates a purchase from a list of submitted items and a payment method.
		/// The items may also specify they are for a subscription by declaring isSubscribing: true instead of quantity.
		/// 
		/// {
		///		paymentMethod: uintId || {gatewayToken: '', gatewayId: uintId},
		///		items: [
		///			{product: uintId, quantity: ulong, isSubscribing: bool}
		///		],
		///		couponCode: x,
		///		future - delivery address etc
		/// }
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost("submit")]
		public virtual async ValueTask Submit([FromBody] JObject purchaseOrder)
		{
			if (purchaseOrder == null || purchaseOrder.Type != JTokenType.Object)
			{
				throw new PublicException("Order details required", "order_details_not_provided");
			}

			var context = await Request.GetContext();
			var anySubscription = false;

			// Parse the items.
			var itemsJson = purchaseOrder["items"] as JArray;

			if (itemsJson == null)
			{
				throw new PublicException("At least one item is required", "items_required");
			}

			foreach (var item in itemsJson)
			{
				var productIdJson = item["product"];

				if (productIdJson == null || productIdJson.Type != JTokenType.Integer)
				{
					throw new PublicException("Items require product field (numeric ID)", "item_product_required");
				}

				if (item["isSubscribing"] != null)
				{
					anySubscription = true;
				}
				else
				{
					var qtyJson = item["quantity"];

					if (qtyJson == null || qtyJson.Type != JTokenType.Integer)
					{
						throw new PublicException("Non subscription items require quantity field (numeric ID)", "item_quantity_required");
					}
				}
			}

			// Parse the payment method. It might not be required if the price is actually free.
			var paymentMethodJson = purchaseOrder["paymentMethod"];

			PaymentMethod paymentMethod = null;

			if (paymentMethodJson != null)
			{
				// Must of course check ownership if an ID is provided.
				if (paymentMethodJson.Type == JTokenType.Integer)
				{
					// Convert to a long:
					var value = paymentMethodJson.ToObject<long>();

					if (value <= 0 || value > uint.MaxValue)
					{
						throw new PublicException("Payment method ID provided but it did not exist", "payment_method_invalid");
					}

					// -> uint:
					var methodId = (uint)value;

					// Get by that ID - dependent on the permission system permitting this:
					paymentMethod = await Services.Get<PaymentMethodService>().Get(context, methodId);

					if (paymentMethod == null)
					{
						throw new PublicException("Payment method ID provided but it did not exist", "payment_method_invalid");
					}

				}
				else if (paymentMethodJson.Type == JTokenType.Object)
				{
					// gatewayToken, gatewayId, name
					// Omit name to not save the card. Saving is required if any of the products are subscriptions.

					var nameJson = paymentMethodJson["name"];
					var gatewayTokenJson = paymentMethodJson["gatewayToken"];
					var gatewayIdJson = paymentMethodJson["gatewayId"];
					var gatewayToken = gatewayTokenJson.ToObject<string>();

					var gatewayId = gatewayIdJson.ToObject<long>();
					
					if (gatewayId <= 0 || gatewayId > uint.MaxValue)
					{
						throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
					}

					if (anySubscription)
					{
						// Name required (must save the payment method)
						if (nameJson == null)
						{
							throw new PublicException("Payment method requires a name when subscribing.", "payment_method_name");
						}
					}

					if (nameJson != null)
					{
						var methodName = nameJson.ToObject<string>();

						paymentMethod = await Services.Get<PaymentMethodService>().Create(context, new PaymentMethod()
						{
							Name = methodName,
							GatewayToken = gatewayToken,
							PaymentGatewayId = (uint)gatewayId
						}, DataOptions.IgnorePermissions);
					}
					else
					{
						// Create a method but don't save it.
						paymentMethod = new PaymentMethod()
						{
							GatewayToken = gatewayToken,
							PaymentGatewayId = (uint)gatewayId
						};
					}
				}
			}

			var productQuantities = Services.Get<ProductQuantityService>();

			// If any subscriptions, create the subscription now.
			if (anySubscription)
			{
				var sub = await Services.Get<SubscriptionService>().Create(context, new Subscription()
				{

					PaymentMethodId = paymentMethod.Id,
					TimeslotFrequency = 0, // Months
					LocaleId = context.LocaleId,
					UserId = context.UserId

				}, DataOptions.IgnorePermissions);

				// Create each object on it:
				foreach (var item in itemsJson)
				{
					var productIdJson = item["product"];
					
					if (item["isSubscribing"] != null)
					{
						var productId = productIdJson.ToObject<long>();

						if (productId < 0 || productId > uint.MaxValue)
						{
							throw new PublicException("Product ID provided but it did not exist", "product_invalid");
						}

						await productQuantities.Create(context, new ProductQuantity() {
							ProductId = (uint)productId,
							Quantity = 1,
							SubscriptionId = sub.Id,
							UserId = context.UserId
						}, DataOptions.IgnorePermissions);
					}
				}
				
			}
			else
			{
				// One off purchase.
				var purchase = await _service.Create(context, new Purchase()
				{
					LocaleId = context.LocaleId,
					PaymentGatewayId = paymentMethod.PaymentGatewayId,
					PaymentMethodId = paymentMethod.Id,
					UserId = context.UserId
				}, DataOptions.IgnorePermissions);
				
				// Create each object on it:
				foreach (var item in itemsJson)
				{
					var productIdJson = item["product"];
					var quantityJson = item["quantity"];

					var productId = productIdJson.ToObject<long>();

					if (productId < 0 || productId > uint.MaxValue)
					{
						throw new PublicException("Product ID provided but it did not exist", "product_invalid");
					}
					
					var quantity = quantityJson.ToObject<ulong>();

					await productQuantities.Create(context, new ProductQuantity()
					{
						ProductId = (uint)productId,
						Quantity = quantity,
						PurchaseId = purchase.Id,
						UserId = context.UserId
					}, DataOptions.IgnorePermissions);
				}

				// Execute it:
				await (_service as PurchaseService).Execute(context, purchase);

			}

		}
	}
}