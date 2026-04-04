using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>Handles purchase endpoints.</summary>
	[Route("v1/purchase")]
	public partial class PurchaseController : AutoController<Purchase>
	{

		/// <summary>
		/// Process the approval challenge response
		/// </summary>
		/// <param name="httpContext"></param>
		/// <param name="context"></param>
		/// <returns></returns>

		[HttpPost("opayo/challenge/callback")]
		public virtual async ValueTask<PurchaseStatus> ValidateChallenge(HttpContext httpContext, Context context)
		{
			var request = httpContext.Request;

			request.Form.TryGetValue("cres", out var response);
			request.Form.TryGetValue("threeDSSessionData", out var token);

			// token is url encoded base64 as it is also passed to the provider
			string unescaped = Uri.UnescapeDataString(token);
			var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(unescaped));

			var challengeResponse = new ChallengeResponse()
			{
				CRes = response,
				Token = decodedToken
			};

			if (string.IsNullOrWhiteSpace(challengeResponse.Token))
			{
				return new PurchaseStatus()
				{
					Status = 500
				};
			}

			// save ip address against the token for auditing
			var ipAddress = RequestHelper.GetClientIp(httpContext);
			var purchase = await (_service as PurchaseService).GetByToken(context, challengeResponse.Token, ipAddress);

			if (purchase == null)
			{
				throw new PublicException("Could not process validation response.", "purchase/purchase_validation_not_found");
			}

			var purchaseAndAction = await (_service as PurchaseService).ValidateChallenge(context, purchase, challengeResponse);

			// only pass back the status
			return new PurchaseStatus()
			{
				Status = purchase != null ? purchase.Status : 500
			};
		}

		/// <summary>
		/// Process the approval challenge response (called from redirect page after hosted payment processing)
		/// </summary>
		/// <param name="httpContext"></param>
		/// <param name="context"></param>
		/// <param name="hostedPageResponse"></param>
		/// <returns></returns>

		[HttpPost("hosted/payment/callback")]
		public virtual async ValueTask<Purchase> ValidateHostedPageResponse(HttpContext httpContext, Context context, [FromBody] HostedPageResponse hostedPageResponse)
		{
			// token is url encoded base64 as it is also passed to the provider
			string unescaped = Uri.UnescapeDataString(hostedPageResponse.Token);
			var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(unescaped));

			if (string.IsNullOrWhiteSpace(decodedToken))
			{
				return null;
			}

			// save ip address against the token for auditing
			var ipAddress = RequestHelper.GetClientIp(httpContext);
			var purchase = await (_service as PurchaseService).GetByToken(context, decodedToken, ipAddress);

			if (purchase == null)
			{
				throw new PublicException("Could not process hosted payment response.", "purchase/purchase_not_found");
			}

			if (purchase.Reference != hostedPageResponse.Reference)
			{
				throw new PublicException("Could not process hosted payment response.", "purchase/reference_not_found");
			}

			return await (_service as PurchaseService).ValidateHostedPayment(context, purchase, hostedPageResponse);
		}

		/// <summary>
		/// Check the purchase status via a token 
		/// Used during additional auth processes such as 3ds
		/// </summary>
		[HttpGet("approval/status/{token}")]
		public async ValueTask<PurchaseStatus> ApprovalStatus(HttpContext httpContext, Context context, [FromRoute] string token)
		{
			// save ip address against the token for auditing
			var ipAddress = RequestHelper.GetClientIp(httpContext);
			var purchase = await (_service as PurchaseService).GetByToken(context, token, ipAddress);

			// only pass back the status
			return new PurchaseStatus()
			{
				Status = purchase != null ? purchase.Status : 500
			};
		}

		/// <summary>
		/// Retrieve a guest purchase via the token (if still valid)
		/// </summary>
		[HttpGet("get/token/{token}")]
		public async ValueTask<Purchase> GetByToken(HttpContext httpContext, Context context, [FromRoute] string token)
		{
			// save ip address against the token for auditing
			var ipAddress = RequestHelper.GetClientIp(httpContext);
			var purchase = await (_service as PurchaseService).GetByToken(context, token, ipAddress);

			return purchase;
		}

		/// <summary>
		/// Resend an email for a purchase (admin only)
		/// </summary>
		[HttpGet("resend/email/{id}")]
		public async ValueTask<bool> ResendEmail(HttpContext httpContext, Context context, [FromRoute] uint id, [FromQuery] string key)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "Web Category/admin_required");
			}

			if (string.IsNullOrWhiteSpace(key))
			{
				key = "payment_order_details";
			}

			var purchase = await (_service as PurchaseService).Get(context, id, DataOptions.IgnorePermissions);


			if (purchase == null)
			{
				return false;
			}

			// resend email
			var processed = false;
			await Events.Purchase.SendConfirmationEmail.Dispatch(context, processed, key, purchase);

			return true;
		}


		/*
		/// <summary>
		/// POST /v1/purchase/submit (Obsolete)
		/// Creates a purchase from a list of submitted items and a payment method.
		/// The items may also specify they are for a subscription by declaring isSubscribing: true instead of quantity.
		/// 
		/// {
		///		paymentMethod: uintId || {gatewayToken: '', gatewayId: uintId},
		///		items: [
		///			{product: uintId, quantity: ulong, isSubscribing: bool}
		///		],
		///		future - delivery address etc
		/// }
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost("submit")]
		public virtual async ValueTask<PurchaseStatus> Submit(Context context, [FromBody] JObject purchaseOrder)
		{
			if (purchaseOrder == null || purchaseOrder.Type != JTokenType.Object)
			{
				throw new PublicException("Order details required", "order_details_not_provided");
			}

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

				prodQuant = await productQuantities.Create(context, prodQuant, DataOptions.IgnorePermissions);

				// Which bucket does this go into?
				if (product.BillingFrequency == 0)
				{
					// It's a one off.
					if (oneOff == null)
					{
						// Create it:
						oneOff = new Purchase()
						{
							LocaleId = context.LocaleId,
							PaymentGatewayId = paymentMethod.PaymentGatewayId,
							PaymentMethodId = paymentMethod.Id,
							UserId = context.UserId
						};
					}

					oneOff.Mappings.Add("PurchaseQuantities", prodQuant);
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
								week = new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 3, // Weeks
									LocaleId = context.LocaleId,
									UserId = context.UserId
								};
							}

							subToUse = week;

							break;
						case 2:
							// Monthly

							if (month == null)
							{
								month = new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 0, // Months
									LocaleId = context.LocaleId,
									UserId = context.UserId
								};
							}

							subToUse = month;

							break;
						case 3:
							// Quarterly

							if (quarter == null)
							{
								quarter = new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 1, // Quarters
									LocaleId = context.LocaleId,
									UserId = context.UserId
								};
							}

							subToUse = quarter;
							break;
						case 4:
							// Annually

							if (year == null)
							{
								year = new Subscription()
								{
									PaymentMethodId = paymentMethod.Id,
									TimeslotFrequency = 2, // Years
									LocaleId = context.LocaleId,
									UserId = context.UserId
								};
							}

							subToUse = year;
							break;
					}

					subToUse.Mappings.Add("ProductQuantities", prodQuant);
				}

			}

			// Next check if we need to do a singular execution or a multi execution.
			var executeCount = 0;

			if (oneOff != null)
			{
				oneOff = await _service.Create(context, oneOff, DataOptions.IgnorePermissions);
				executeCount++;
			}

			if (week != null)
			{
				week = await subscriptions.Create(context, week, DataOptions.IgnorePermissions);
				executeCount++;
			}

			if (month != null)
			{
				month = await subscriptions.Create(context, month, DataOptions.IgnorePermissions);
				executeCount++;
			}

			if (quarter != null)
			{
				quarter = await subscriptions.Create(context, quarter, DataOptions.IgnorePermissions);
				executeCount++;
			}

			if (year != null)
			{
				year = await subscriptions.Create(context, year, DataOptions.IgnorePermissions);
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
		*/
	}
}