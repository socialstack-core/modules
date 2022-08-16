using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
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
		public virtual async ValueTask<PurchaseStatus> Submit([FromBody] JObject purchaseOrder)
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

			PaymentMethod paymentMethod;
			Purchase purchase = null;
			PurchaseAndAction purchaseAction;

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
					// gatewayToken, gatewayId, save: true|false
					// Saving is required and inferred to be true if any of the products are subscriptions.

					var nameJson = paymentMethodJson["name"];
					var expiryJson = paymentMethodJson["expiry"];
					var issuerJson = paymentMethodJson["issuer"];
					var gatewayTokenJson = paymentMethodJson["gatewayToken"];
					var gatewayIdJson = paymentMethodJson["gatewayId"];
					var gatewayToken = gatewayTokenJson.ToObject<string>();

					var gatewayId = gatewayIdJson.ToObject<long>();

					if (gatewayId <= 0 || gatewayId > uint.MaxValue)
					{
						throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
					}

					var saveable = nameJson != null && expiryJson != null && issuerJson != null;

					if (anySubscription)
					{
						// Saving is required if a product is a subscription.
						if (!saveable)
						{
							throw new PublicException("name, expiry and issuer required when adding a new subscription payment method", "payment_method_missing_data");
						}
					}

					// Get the payment gateway:
					var gateway = Services.Get<PaymentGatewayService>().Get((uint)gatewayId);

					if (gateway == null)
					{
						throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
					}

					// Ask the gateway to convert the gateway token if it needs to do so.
					gatewayToken = await gateway.PrepareToken(context, gatewayToken);

					if (saveable)
					{
						var name = nameJson.ToString();
						var expiryUtc = expiryJson.ToObject<DateTime>();
						var issuer = issuerJson.ToString();

						paymentMethod = await Services.Get<PaymentMethodService>().Create(context, new PaymentMethod()
						{
							Issuer = issuer,
							UserId = context.UserId,
							Name = name,
							ExpiryUtc = expiryUtc,
							LastUsedUtc = DateTime.UtcNow,
							GatewayToken = gatewayToken,
							PaymentGatewayId = gateway.Id
						}, DataOptions.IgnorePermissions);
					}
					else
					{
						// Create a method but don't save it.
						paymentMethod = new PaymentMethod()
						{
							UserId = context.UserId,
							LastUsedUtc = DateTime.UtcNow,
							GatewayToken = gatewayToken,
							PaymentGatewayId = gateway.Id
						};
					}
				}
				else
				{
					throw new PublicException("Payment method ID provided but it was an invalid type", "payment_method_invalid");
				}
			}
			else
			{
				throw new PublicException("Payment method missing", "payment_method_required");
			}

			var productQuantities = Services.Get<ProductQuantityService>();

			// If any subscriptions, create the subscription now.
			if (anySubscription)
			{
				var subscriptions = Services.Get<SubscriptionService>();

				var sub = await subscriptions.Create(context, new Subscription()
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

				// Charge the subscription now:
				purchaseAction = await subscriptions.ChargeSubscription(context, sub);

			}
			else
			{
				// One off purchase.
				purchase = await _service.Create(context, new Purchase()
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
				purchaseAction = await (_service as PurchaseService).Execute(context, purchase);

			}

			return new PurchaseStatus() {
				Status = purchaseAction.Purchase.Status,
				NextAction = purchaseAction.Action
			};

		}
	}
}