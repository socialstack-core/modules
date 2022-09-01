using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

			// Check the items first to avoid creating a payment method if we have invalid items.
			foreach (var item in itemsJson)
			{
				var productIdJson = item["product"];

				if (productIdJson == null || productIdJson.Type != JTokenType.Integer)
				{
					throw new PublicException("Items require product field (numeric ID)", "item_product_required");
				}

				var qtyJson = item["quantity"];

				if (qtyJson == null || qtyJson.Type != JTokenType.Integer)
				{
					throw new PublicException("Non subscription items require quantity field (numeric ID)", "item_quantity_required");
				}
			}

			// Parse the payment method. It might not be required if the price is actually free.
			var paymentMethodJson = purchaseOrder["paymentMethod"];

			PaymentMethod paymentMethod;
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

			var couponCodeJson = purchaseOrder["couponCode"];
			Coupon coupon = null;

			if (couponCodeJson != null)
			{
				if (couponCodeJson.Type != JTokenType.String)
				{
					throw new PublicException("Coupon code provided but it was an invalid type", "coupon_invalid");
				}

				var couponCode = couponCodeJson.ToString();

				// Attempt to get the coupon:
				coupon = await Services.Get<CouponService>().Where("Token=?", DataOptions.IgnorePermissions).Bind(couponCode).First(context);
			}

			var productQuantities = Services.Get<ProductQuantityService>();

			// Next, for each requested product, establish if it needs to create a subscription as well.
			// There are potentially 4 different subscription objects that can be created at once (week, month, quarter, year).

			Subscription week = null;
			Subscription month = null;
			Subscription quarter = null;
			Subscription year = null;
			Purchase oneOff = null;

			// If any subscriptions, create the subscription now.
			var subscriptions = Services.Get<SubscriptionService>();
			var products = Services.Get<ProductService>();

			foreach (var item in itemsJson)
			{
				var productIdJson = item["product"];
				var quantityJson = item["quantity"];

				var productId = productIdJson.ToObject<long>();

				if (productId < 0 || productId > uint.MaxValue)
				{
					throw new PublicException("Product ID provided but it did not exist", "product_invalid");
				}

				// Get the product:
				var product = await products.Get(context, (uint)productId, DataOptions.IgnorePermissions);

				if (product == null)
				{
					throw new PublicException("Product ID provided but it did not exist", "product_invalid");
				}

				var quantity = quantityJson.ToObject<ulong>();

				var prodQuant = new ProductQuantity()
				{
					ProductId = (uint)productId,
					Quantity = quantity,
					UserId = context.UserId
				};

				// Which bucket does this go into?
				if (product.BillingFrequency == 0)
				{
					// It's a one off.
					if (oneOff == null)
					{
						// Create it:
						oneOff = await _service.Create(context, new Purchase()
						{
							LocaleId = context.LocaleId,
							PaymentGatewayId = paymentMethod.PaymentGatewayId,
							PaymentMethodId = paymentMethod.Id,
							UserId = context.UserId
						}, DataOptions.IgnorePermissions);
					}

					prodQuant.PurchaseId = oneOff.Id;
				}
				else
				{
					Subscription subToUse = null;

					switch (product.BillingFrequency)
					{
						case 1:
							// Weekly
							if (week == null)
							{
								week = await subscriptions.Create(context, new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 3, // Weeks
									LocaleId = context.LocaleId,
									UserId = context.UserId
								}, DataOptions.IgnorePermissions);
							}

							subToUse = week;

							break;
						case 2:
							// Monthly
							
							if (month == null)
							{
								month = await subscriptions.Create(context, new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 0, // Months
									LocaleId = context.LocaleId,
									UserId = context.UserId
								}, DataOptions.IgnorePermissions);
							}

							subToUse = month;

							break;
						case 3:
							// Quarterly

							if (quarter == null)
							{
								quarter = await subscriptions.Create(context, new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 1, // Quarters
									LocaleId = context.LocaleId,
									UserId = context.UserId
								}, DataOptions.IgnorePermissions);
							}

							subToUse = quarter;
							break;
						case 4:
							// Annually

							if (year == null)
							{
								year = await subscriptions.Create(context, new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 2, // Years
									LocaleId = context.LocaleId,
									UserId = context.UserId
								}, DataOptions.IgnorePermissions);
							}

							subToUse = year;
							break;
					}

					prodQuant.SubscriptionId = subToUse.Id;
				}

				await productQuantities.Create(context, prodQuant, DataOptions.IgnorePermissions);
			}

			// Next check if we need to do a singular execution or a multi execution.
			var executeCount = 0;

			if (oneOff != null)
			{
				executeCount++;
			}

			if (week != null)
			{
				executeCount++;
			}

			if (month != null)
			{
				executeCount++;
			}
			
			if (quarter != null)
			{
				executeCount++;
			}
			
			if (year != null)
			{
				executeCount++;
			}

			if (executeCount == 1)
			{
				// Most common situation. We're executing a single one off purchase or a singular subscription.

				if (oneOff != null)
				{
					// Execute it:
					purchaseAction = await (_service as PurchaseService).Execute(context, oneOff, paymentMethod, coupon);
				}
				else
				{
					// Execute a subscription:
					Subscription sub = null;

					if (year != null)
					{
						sub = year;
					}
					else if (quarter != null)
					{
						sub = quarter;
					}
					else if (month != null)
					{
						sub = month;
					}
					else if (week != null)
					{
						sub = week;
					}

					purchaseAction = await subscriptions.ChargeSubscription(context, sub, coupon);
				}

			}
			else
			{
				// Multi execution. This is where somebody added multiple types of product to their cart at the same time.
				// For example, a one off purchase, a monthly subscription and a yearly subscription.
				// A special MultiExecute endpoint exists for this situation where a singular one off payment is made and in its success
				// 1 or more subscriptions will be ticked.

				List<Subscription> subscriptionSet = null;

				if (week != null)
				{
					if (subscriptionSet == null)
					{
						subscriptionSet = new List<Subscription>();
					}
					subscriptionSet.Add(week);
				}
				
				if (month != null)
				{
					if (subscriptionSet == null)
					{
						subscriptionSet = new List<Subscription>();
					}
					subscriptionSet.Add(month);
				}
				
				if (quarter != null)
				{
					if (subscriptionSet == null)
					{
						subscriptionSet = new List<Subscription>();
					}
					subscriptionSet.Add(quarter);
				}
				
				if (year != null)
				{
					if (subscriptionSet == null)
					{
						subscriptionSet = new List<Subscription>();
					}
					subscriptionSet.Add(year);
				}

				purchaseAction = await (_service as PurchaseService).MultiExecute(context, oneOff, subscriptionSet, paymentMethod, coupon);
			}

			return new PurchaseStatus() {
				Status = purchaseAction.Purchase.Status,
				NextAction = purchaseAction.Action
			};

		}
	}
}