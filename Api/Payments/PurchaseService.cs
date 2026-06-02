using Api.CanvasRenderer;
using Api.Contexts;
using Api.Counters;
using Api.Emails;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if PAYMENTS_GUEST_USERS
using Api.GuestUsers;
#endif

namespace Api.Payments
{
	/// <summary>
	/// Handles purchases.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PurchaseService : AutoService<Purchase>
	{
		private PaymentMethodService _paymentMethods;
		private PaymentGatewayService _gateways;
		private ProductQuantityService _prodQuantities;
		private ProductService _products;
		private PurchaseTokenService _purchaseTokens;
		private DeliveryService _deliveries;
		private UserService _users;
		private EmailTemplateService _emails;
		private CounterService _counters;
		private PriceServiceConfig _priceConfig;
		private PriceService _prices;
#if PAYMENTS_GUEST_USERS
		private GuestUserService _guestUsers;
#endif

		// removed any similar chars like 1/ I 5/S 0/O etc 
		private const string refPattern = "ACEFHJKMNPRTUVWXY23456789";

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseService(PaymentMethodService paymentMethods, PageService pages,
			PaymentGatewayService gateways, ProductQuantityService prodQuantities,
			ProductService products, PurchaseTokenService purchaseTokens, EmailTemplateService emailTemplateService, PriceService prices,
			UserService users, LocaleService locales, DeliveryService deliveries, CounterService counters) : base(Events.Purchase)
		{
			_paymentMethods = paymentMethods;
			_gateways = gateways;
			_prodQuantities = prodQuantities;
			_products = products;
			_purchaseTokens = purchaseTokens;
			_users = users;
			_deliveries = deliveries;
			_emails = emailTemplateService;
			_counters = counters;
			_priceConfig = GetConfig<PriceServiceConfig>();
			_prices = prices;

			InstallAdminPages("Orders", "fa:fa-shopping-basket", ["id", "reference", "totalCost", "totalCostLessTax"], null, "ecommerce");

			pages.Install(
				new PageBuilder()
				{
					Url = "/cart/",
					Key = "cart_view",
					Title = "View your shopping cart",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Payments/Cart")
						);
					}
				},
				new PageBuilder()
				{
					Url = "/cart/complete",
					Key = "cart_complete",
					Title = "Checkout completion",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Payments/Complete")
						);
					}
				},
				new PageBuilder()
				{
					Url = "/cart/checkout",
					Key = "cart_checkout",
					Title = "Review your cart",
					LoginRequired = true,
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Payments/Checkout")
						);
					}
				},
				new PageBuilder()
				{
					Url = "/cart/purchases/${purchase.id}",
					Key = "primary:purchase",
					Title = "Viewing purchase",
					LoginRequired = true,
					PrimaryContentIncludes = "productQuantities",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Payments/Purchase/View").WithPrimaryLink("purchase")
						);
					}
				},
				new PageBuilder()
				{
					Url = "/cart/purchases/token/${token}",
					Key = "payment_order_by_token",
					Title = "Purchase",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Guest/Purchase")
								.WithLink("token", "url.token", false)
						);
					}
				},
				new PageBuilder()
				{
					Url = "/cart/purchases/hosted/${token}",
					Key = "payment_order_hosted_status",
					Title = "Purchase",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Payments/HostedStatus")
								.WithLink("token", "url.token", false)
						);
					}
				}
			);

			InstallEmails(
				new EmailBuilder()
				{
					Name = "Your payment receipt",
					Subject = "Your payment receipt",
					Key = "payment_receipt",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"Thank you! A payment was successfully made for "
							)
							.AppendChild(
								new CanvasNode("UI/Token")
									.With("mode", "customdata")
									.With("fields", new string[] { "printablePrice" })
									.AppendChild("${customData.printablePrice}")
							)
							.AppendChild(
								new CanvasNode("Email/PrimaryButton")
									.With("label", "View payment details")
									.With("target", "/checkout/payment/${customData.paymentId}")
							)
						);
					}
				},
				new EmailBuilder()
				{
					Name = "A payment issue occurred",
					Subject = "A payment issue occurred",
					Key = "payment_fault",
					PrimaryContentType = "PurchaseToken",
					PrimaryContentIncludes = "purchase, purchase.productQuantities, purchase.productQuantities.product, purchase.billingAddress, purchase.deliveryAddress",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"There was an issue with the payment for this order"
							)
							.AppendChild(
								new CanvasNode("Email/Purchase/View")
									.WithPrimaryLink("purchaseToken")
							)
							.AppendChild(
								new CanvasNode("Email/PrimaryButton")
									.With("label", "View your order details online")
									.With("target", "cart/purchases/token/${customData.token}")
							)
						);
					}
				},
				new EmailBuilder()
				{
					Name = "Your order details",
					Subject = "Your order details",
					Key = "payment_order_details",
					PrimaryContentType = "PurchaseToken",
					PrimaryContentIncludes = "purchase, purchase.productQuantities, purchase.productQuantities.product, purchase.billingAddress, purchase.deliveryAddress",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"Thank you for your order"
							)
							.AppendChild(
								new CanvasNode("Email/Purchase/View")
									.WithPrimaryLink("purchaseToken")
							)
							.AppendChild(
								new CanvasNode("Email/PrimaryButton")
									.With("label", "View your order details online")
									.With("target", "cart/purchases/token/${customData.token}")
							)
						);
					}
				}
			);

			Events.Purchase.BeforeCreate.AddEventListener(async (Context context, Purchase purchase) =>
			{
				if (purchase == null)
				{
					return purchase;
				}

				if (!purchase.BuyNowPayLater)
				{
					// Ensure paymentGatewayId is set:
					await EnsureGatewayId(context, purchase);
				}

				// Ensure a locale is set:
				if (purchase.LocaleId == 0)
				{
					purchase.LocaleId = context.LocaleId;
				}

				if (string.IsNullOrWhiteSpace(purchase.Reference))
				{
					purchase.Reference = await _counters.GetCounter("PurchaseId", "WEB");
				}

				return purchase;
			});

			Events.Purchase.AfterUpdate.AddEventListener(async (Context context, Purchase purchase, ChangedFields diff) =>
			{
				if (!diff.HasChanged("Status"))
				{
					return purchase;
				}

				if (purchase.Status == 201) // Just send order confirmation for BNPL
				{
					// send order confirmation email
					var processed = false;
					await Events.Purchase.SendConfirmationEmail.Dispatch(context, processed, "payment_order_details", purchase);
				}
				else if (purchase.Status == 202) // Don't include BNPL here (201) as the payment doesn't actually happen until later.
				{
					// send order payment success email
					var processed = false;
					await Events.Purchase.SendConfirmationEmail.Dispatch(context, processed, "payment_receipt", purchase);

					// send order confirmation email
					processed = false;
					await Events.Purchase.SendConfirmationEmail.Dispatch(context, processed, "payment_order_details", purchase);
				}
				else if (purchase.Status > 299)
				{
					// send order payment failure  email
					var processed = false;
					await Events.Purchase.SendConfirmationEmail.Dispatch(context, processed, "payment_fault", purchase);
				}

				// Check to see if we can validate the card as being used
				if (purchase.Status == 202 && purchase.PaymentMethodId > 0)
				{
					var paymentMethod = await _paymentMethods.Get(context, purchase.PaymentMethodId, DataOptions.IgnorePermissions);

					if (paymentMethod != null && !paymentMethod.IsValidated)
					{
						paymentMethod = await _paymentMethods.Update(context, paymentMethod, (Context ctx, PaymentMethod toUpdate, PaymentMethod orig) =>
						{
							toUpdate.IsValidated = true;
						}, DataOptions.IgnorePermissions);
					}
				}

				return purchase;
			}); 

			Events.Purchase.SendConfirmationEmail.AddEventListener(async (Context context, bool processed, string key, Purchase purchase) =>
			{
				if (processed || string.IsNullOrWhiteSpace(key) || purchase == null)
				{
					return processed;
				}

				var userRecipient = new Recipient(
					purchase.UserId,
					purchase.LocaleId
				);

				var token = await _purchaseTokens.Create(context,
					new PurchaseToken()
					{
						PurchaseId = purchase.Id,
						Scope = "View",
						IsSingleUse = false,
						CreatedUtc = DateTime.UtcNow,
						ExpiresUtc = DateTime.UtcNow.AddDays(14)
					}, DataOptions.IgnorePermissions);

				if (token == null)
				{
					throw new PublicException("Could not create session token.", "Purchase_session_token");
				}

				if (key == "payment_order_details" || key == "payment_fault")
				{
					userRecipient.CustomData = token;
				}
				else
				{
					// Send success email:
					userRecipient.CustomData = new
					{
						Purchase = purchase,
						PrintablePrice = PrintPrice(purchase.TotalCost, purchase.CurrencyCode),
						Token = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Token)))
					};
				}

				_emails.Send(userRecipient, key);

				// processed so return null
				return true;
			});
			
#if PAYMENTS_GUEST_USERS
			// link the purchase to the guest user
			Events.Purchase.Checkout.AddEventListener(async (Context context, Purchase purchase, CheckoutInfo checkoutInfo) =>
			{
				// only anon users should be checking out as a quest
				if (context.UserId != 0 || context.RoleId != 6)
				{
					return purchase;
				}

				if (purchase == null || checkoutInfo.GuestUserId == 0)
				{
					return purchase;
				}

				purchase.GuestUserId = checkoutInfo.GuestUserId;

				return purchase;
			}, 20);

			// send any emails to the guest user 
			Events.Purchase.SendConfirmationEmail.AddEventListener(async (Context context, bool processed, string key, Purchase purchase) => {

				if (processed || string.IsNullOrWhiteSpace(key) || purchase == null || purchase.GuestUserId == 0)
				{
					return processed;
				}

				//for guest users only send order confirmation
				if (key != "payment_order_details" && key != "payment_fault")
				{
					return true;
				}

				_guestUsers ??= Services.Get<GuestUserService>();

				var guestUser = await _guestUsers.Get(context, purchase.GuestUserId.GetValueOrDefault(), DataOptions.IgnorePermissions);

				if (guestUser == null || string.IsNullOrWhiteSpace(guestUser.Email))
				{
					return processed;
				}

				// send email to guest passing token to allow for lookup/retrieval
				var userRecipient = new Recipient(guestUser.Email);

				var token = await _purchaseTokens.Create(context,
					new PurchaseToken()
					{
						PurchaseId = purchase.Id,
						Scope = "View",
						IsSingleUse = false,
						CreatedUtc = DateTime.UtcNow,
						ExpiresUtc = DateTime.UtcNow.AddDays(14)
					}, DataOptions.IgnorePermissions);

				if (token == null)
				{
					throw new PublicException("Could not create session token.", "Purchase_session_token");
				}

				userRecipient.CustomData = token;

				_emails.Send(userRecipient, key);

				// processed so return null
				return true;
			}, 5); // run before any stock listeners and sets processed to block others 
#endif
		}

		/// <summary>
		/// helper class to append to response logging
		/// </summary>
		/// <param name="jsonString"></param>
		/// <param name="newElement"></param>
		/// <returns></returns>
		public string AppendResponseElement(string jsonString, object newElement)
		{
			if (newElement == null)
			{
				throw new ArgumentNullException(nameof(newElement), "New element cannot be null.");
			}

			JObject elementObject;
			try
			{
				// If it's already a JObject, use it; otherwise try to convert the POCO to a JObject.
				elementObject = newElement as JObject ?? JObject.FromObject(newElement);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("newElement must be convertible to a JSON object (JObject).", nameof(newElement), ex);
			}

			if (string.IsNullOrWhiteSpace(jsonString))
			{
				var newArray = new JArray { elementObject };
				return newArray.ToString(Formatting.None);
			}

			JToken token;
			try
			{
				token = JToken.Parse(jsonString);
			}
			catch (JsonReaderException ex)
			{
				throw new ArgumentException("Invalid JSON in jsonString.", nameof(jsonString), ex);
			}

			JArray array = token as JArray ?? new JArray { token };

			array.Add(elementObject);
			return array.ToString(Formatting.None);
		}

		/// <summary>
		/// Get a purcase via the onetime token
		/// </summary>
		/// <param name="context"></param>
		/// <param name="token"></param>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		public async ValueTask<Purchase> GetByToken(Context context, string token, string ipAddress = null)
		{
			var purchaseToken = await _purchaseTokens.Get(context, token, ipAddress);

			if (purchaseToken == null)
			{
				return null;
			}

			return await Get(context, purchaseToken.PurchaseId, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Gets the products in the given purchase.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProductQuantities(Context context, Purchase purchase)
		{
			return await _prodQuantities
				.ListBySource(context, purchase, "ProductQuantities", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Gets the requested products in the given purchase. Null if the purchase has not been modified.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetOriginalProductQuantities(Context context, Purchase purchase)
		{
			return await _prodQuantities
				.ListBySource(context, purchase, "RequestedProductQuantities", DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Clones the purchase set of product/quants in to a set called 'RequestedProductQuantities'.
		/// This is used to save a snapshot of the original purchase items for changes and returns etc 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<Purchase> CloneToRequestedProductQuantities(Context context, Purchase purchase)
		{
			var existing = await GetOriginalProductQuantities(context, purchase);
			if (existing != null && existing.Count > 0)
			{
				return purchase;
			}

			// Not yet - clone them now. *must* clone them as the objects will be modified.
			var originalRequests = await GetProductQuantities(context, purchase);

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

				var result = await _prodQuantities.Create(context, purchaseQuantity, DataOptions.IgnorePermissions);

				if (result == null)
				{
					continue;
				}

				ids.Add(result.Id);
			}

			return await Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase original) =>
			{
				toUpdate.Mappings.Set("RequestedProductQuantities", ids);

			}, DataOptions.IgnorePermissions);
		}

		private async ValueTask EnsureGatewayId(Context context, Purchase purchase)
		{

			if (purchase.PaymentGatewayId != 0 && purchase.PaymentMethodId == 0)
			{
				// method known but has no saved payment details (guest or not configured to save cards)
				return;
			}

			if (purchase.PaymentMethodId == 0)
			{
				throw new PublicException("No saved payment details specified.", "payment_gateway_required");
			}

			// Get the payment method (must be reachable by the context):
			var paymentMethod = await _paymentMethods.Get(context, purchase.PaymentMethodId);

			if (paymentMethod == null)
			{
				// Probably tried to use some other payment method ID.
				throw new PublicException("No payment method specified.", "payment_method_required");
			}

			var gatewayId = paymentMethod.PaymentGatewayId;
			purchase.PaymentGatewayId = gatewayId;

			var gateway = _gateways.Get(gatewayId);

			if (gateway == null)
			{
				throw new PublicException(
					"The gateway that your payment method is through is currently unavailable. If this keeps happening, please let us know.",
					"payment_method_unavailable"
				);
			}
		}

		/// <summary>
		/// Adds the given product quantities to the given purchase. Does not check if they have already been added.
		/// This happens via duplicating the given product quantities to avoid any risk of cart manipulation during checkout.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="toPurchase"></param>
		/// <param name="productAndPricing"></param>
		/// <param name="mappingName"></param>
		/// <returns>True if at least 1 was added.</returns>
		public async ValueTask<List<LineItem>> AddProductsUnsaved(Context context, Purchase toPurchase,
			ProductQuantityPricing productAndPricing,
			string mappingName = "ProductQuantities")
		{
			if (productAndPricing == null || toPurchase == null)
			{
				return null;
			}

			var contents = productAndPricing.Contents;

			if (contents == null || contents.Count == 0)
			{
				return null;
			}

			var set = new List<LineItem>();

			foreach (var lineItem in contents)
			{
				// Must clone it and set the ordered totals:
				var purchaseQuantity = new ProductQuantity()
				{
					ProductId = lineItem.ProductId,
					Quantity = lineItem.Quantity,
					OrderedCurrencyCode = productAndPricing.CurrencyCode,
					OrderedTotal = lineItem.Total,
					OrderedTotalLessTax = lineItem.TotalLessTax
				};

				var result = await _prodQuantities.Create(context, purchaseQuantity, DataOptions.IgnorePermissions);

				if (result == null)
				{
					continue;
				}

				// Add to mapping (unsaved):
				toPurchase.Mappings.Add(mappingName, result);

				set.Add(new LineItem(lineItem, result));
			}

			return set;
		}

		/*
		/// <summary>
		/// Exceutes potentially multiple subscriptions in one transaction. For example if someone wants to buy an annual and monthly subscription at the same time.
		/// This could also be whilst paying a one off amount too (in the provided purchase, which can be null).
		/// If a purchase is provided, the items from the subscription(s) will be copied to it and executed together.
		/// Otherwise, a purchase will be created and everything will be added to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="subscriptions"></param>
		/// <param name="paymentMethod"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> MultiExecute(Context context, List<Subscription> subscriptions, PaymentMethod paymentMethod)
		{
			if (subscriptions == null || subscriptions.Count == 0)
			{
				throw new Exception("No subs!");
			}

			if (_subscriptions == null)
			{
				_subscriptions = Services.Get<SubscriptionService>();
			}

			var locale = await context.GetLocale();

			// Create a purchase:
			var purchase = new Purchase()
			{
				LocaleId = locale.Id,
				MultiExecute = true,
				CurrencyCode = locale.CurrencyCode,
				TaxJurisdiction = subscriptions[0].TaxJurisdiction,
				PaymentGatewayId = paymentMethod.PaymentGatewayId,
				PaymentMethodId = paymentMethod.Id,
				UserId = context.UserId
			};
			
			if (subscriptions != null)
			{
				purchase.Mappings.Set("subscriptions", GetSubscriptionIds(subscriptions));
			}

			// Copy the items from the subs to the purchase:
			foreach (var subscription in subscriptions)
			{
				if (subscription == null)
				{
					continue;
				}

				// Get its items, clone to purchase:
				var inSub = await _subscriptions.GetProducts(context, subscription);
				await AddProductsUnsaved(context, purchase, inSub);
			}

			// Save & create:
			purchase = await Create(context, purchase, DataOptions.IgnorePermissions);
			
			// Attempt to fulfil the purchase now:
			return await Execute(context, purchase, paymentMethod);
		}
		
		/// <summary>
		/// Gets the set of subscription IDs suitable for a mapping.
		/// </summary>
		/// <param name="subscriptions"></param>
		/// <returns></returns>
		private List<ulong> GetSubscriptionIds(List<Subscription> subscriptions)
		{
			var set = new List<ulong>();

			foreach (var subscription in subscriptions)
			{
				set.Add(subscription.Id);
			}

			return set;
		}
		*/

		/// <summary>
		/// Creates a purchase and immediately proceeds to executing it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pricingInfo"></param>
		/// <param name="contentTypeName"></param>
		/// <param name="contentId"></param>
		/// <param name="paymentMethod"></param>
		/// <param name="excludeTax">Only usable if the customers location is zero rated relative to the shop. 
		/// For example, a UK VAT registered business selling to another VAT registered business in the EU can exclude tax but must use the VEIS system.
		/// UK B2B is not tax exempt in this way.</param>
		/// <param name="dupeKey"></param>
		/// <param name="checkoutInfo"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> CreateAndExecute(
			Context context, ProductQuantityPricing pricingInfo,
			string contentTypeName, uint contentId,
			PaymentMethod paymentMethod, bool excludeTax,
			CheckoutInfo checkoutInfo,
			ulong dupeKey = 0)
		{
			var locale = await context.GetLocale();

			// Throw if the info has any error info in it (such as invalid coupons, pricing issues etc).
			_prodQuantities.RequireNoErrors(pricingInfo);

			if (checkoutInfo.DeliveryAddress == null)
			{
				checkoutInfo.DeliveryAddress = await checkoutInfo.GetDeliveryAddress(context);
			}

			if (checkoutInfo.BillingAddress == null)
			{
				checkoutInfo.BillingAddress = await checkoutInfo.GetBillingAddress(context);
			}

			if (checkoutInfo.DeliveryOption == null)
			{
				checkoutInfo.DeliveryOption = await checkoutInfo.GetDeliveryOption(context);
			}

			var purchase = new Purchase()
			{
				UserId = context.UserId,
				TaxJurisdiction = pricingInfo.TaxJurisdiction,
				CouponId = pricingInfo.CouponId,
				CurrencyCode = pricingInfo.CurrencyCode,
				ContentType = contentTypeName,
				ContentId = contentId,
				ContentAntiDuplication = dupeKey,
				HasSubscriptions = pricingInfo.HasSubscriptionProducts,
				// Tax exclusions are highly regulated. Do not use unless you know the relevant laws.
				ExcludeTax = excludeTax,
				ProductsCost = pricingInfo.Total,
				ProductsCostLessTax = pricingInfo.TotalLessTax,
				DeliveryAddressId = checkoutInfo.DeliveryAddress != null ? checkoutInfo.DeliveryAddress.Id : 0,
				BillingAddressId = checkoutInfo.BillingAddress != null ? checkoutInfo.BillingAddress.Id : 0,
				DeliveryOptionId = checkoutInfo.DeliveryOption != null ? checkoutInfo.DeliveryOption.Id : 0,
				BuyNowPayLater = paymentMethod == null,
				PaymentMethodId = paymentMethod == null ? 0 : paymentMethod.Id,
				PaymentGatewayId = paymentMethod == null ? 0 : paymentMethod.PaymentGatewayId,
				IpAddress = checkoutInfo.IpAddress,
				Reference = checkoutInfo.Reference,
				ContactName = checkoutInfo.ContactName,
				CustomerOrderReference = checkoutInfo.CustomerOrderReference,
				DeliveryInformation = checkoutInfo.DeliveryInformation
			};

			// Handle any custom checkout fields:
			await Events.Purchase.Checkout.Dispatch(context, purchase, checkoutInfo);

			// Calculate delivery if any.
			var deliveryInfo = pricingInfo.DeliveryPricingInfo;

			// Copying in the product set - this creates new ones, it *does not* use the same PQ Id.
			// This is to avoid modding the quantity during the payment being processed:
			var purchaseLineItems = await AddProductsUnsaved(context, purchase, pricingInfo);

			if (checkoutInfo.DeliveryOption == null)
			{
				// Delivery cost is simply zero. This option includes both collection and digital goods only orders.
				purchase.DeliveryCost = 0;
				purchase.DeliveryCostLessTax = 0;
			}
			else
			{
				purchase.DeliveryApportionment = deliveryInfo.Value.TaxApportion;

				// Ask delivery option service for the prices it stated.
				var deliveryEstimate = await _deliveries.GetEstimate(context, checkoutInfo.DeliveryOption);

				if (deliveryEstimate == null)
				{
					throw new PublicException("A delivery option is required but one was not provided.", "delivery/required");
				}

				purchase.DeliveryCost = deliveryEstimate.Price;
				purchase.DeliveryCostLessTax = deliveryEstimate.PriceLessTax;

				// Init deliveries on this new purchase:
				purchase.InitialDeliveryData = await _deliveries.SetupDeliveries(context, purchase, purchaseLineItems, deliveryEstimate);
			}

			purchase.TotalCostLessTax = purchase.ProductsCostLessTax + purchase.DeliveryCostLessTax;

			if(_priceConfig.TaxLineByLine)
			{
				//We are calculating tax line by line so this is acceptable
				purchase.TotalCost = purchase.ProductsCost + purchase.DeliveryCost;
			}
			else
			{
				//We are calculating tax based on the TotalCostLessTax so we need to recalculate tax with the delivery included to avoid rounding errors
				// Get tax calc:
				var taxCalc = await _prices.GetTaxCalculator(context, purchase.TaxJurisdiction);

				if(taxCalc == null)
				{
					throw new PublicException("Could not get TaxCalculator", "Purchase/null_taxcalculator");
				}

				purchase.TotalCost = taxCalc.Apply(pricingInfo, purchase.DeliveryCostLessTax, false);
			}
			
			var toPay = excludeTax ? purchase.TotalCostLessTax : purchase.TotalCost;
			purchase.Status = paymentMethod == null ? (uint)(toPay == 0 ? 202 : 201) : 0; // Straight to completion. Free stuff goes to 202, bnpl unpaid 201.

			purchase = await Events.Purchase.BeforeExecuteCreate.Dispatch(context, purchase, pricingInfo, paymentMethod);

			// Save & create:
			purchase = await Create(context, purchase, DataOptions.IgnorePermissions);

			// Attempt to fulfil it immediately:
			return await Execute(context, purchase, paymentMethod);
		}

		/// <summary>
		/// Requests execution of the given payment.
		/// This is triggered by the frontend after the given purchase has had a payment method attached to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> Execute(Context context, Purchase purchase)
		{
			var paymentMethod = purchase.PaymentMethodId == 0 ? null :
				await _paymentMethods.Get(context, purchase.PaymentMethodId, DataOptions.IgnorePermissions);

			return await Execute(context, purchase, paymentMethod);
		}

		/// <summary>
		/// Gets a purchase by a delivery entity, does a reverse lookup on a mapping table.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="delivery"></param>
		/// <param name="dataOptions"></param>
		/// <returns></returns>
		public async ValueTask<Purchase> GetPurchaseByDelivery(Context context, Delivery delivery, DataOptions dataOptions = DataOptions.Default)
		{
			var items = await ListByTarget<Purchase, uint>(context, delivery.Id, "Deliveries", dataOptions);
			return items?.FirstOrDefault();
		}

		/// <summary>
		/// Requests execution of the given payment. Internally calculates the total.
		/// This is triggered by the frontend after the given purchase has had a payment method attached to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="paymentMethod"></param>
		/// <returns></returns>
		private async ValueTask<PurchaseAndAction> Execute(Context context, Purchase purchase, PaymentMethod paymentMethod = null)
		{
			// Event to indicate the purchase is about to execute:
			await Events.Purchase.BeforeExecute.Dispatch(context, purchase);

			// If the total is free, we complete immediately, unless it's the first of a subscription payment.
			// If first subscription payment, must authorise the card.
			PaymentGateway gateway;

			if (paymentMethod == null || purchase.Status == 203)
			{
				// Deferred execution - either BNPL or the order requires review.
				return new PurchaseAndAction()
				{
					Purchase = purchase
				};
			}

			var toPay = purchase.ExcludeTax ? purchase.TotalCostLessTax : purchase.TotalCost;

			if (toPay == 0)
			{
				if (purchase.HasSubscriptions)
				{
					// Get the gateway:
					gateway = _gateways.Get(purchase.PaymentGatewayId);

					if (gateway == null)
					{
						throw new PublicException(
							"The gateway providing your payment method is currently unavailable. If this keeps happening please let us know.",
							"gateway_unavailable"
						);
					}

					if (purchase.CurrencyCode == null)
					{
						throw new PublicException(
							"Whoops! Sorry, we messed up. A currency code was missing from a free subscription purchase. It's required to make sure your bank knows what currency we'll be using. If this keeps happening, please let us know.",
							"currency_missing"
						);
					}

					// Ask the gateway to authorise:
					return await gateway.AuthorisePurchase(purchase, new ProductCost()
					{
						// Legal liability danger - do not set ExcludeTax to true unless you know what you are doing.
						Amount = toPay,
						AmountLessTax = purchase.TotalCostLessTax,
						CurrencyCode = purchase.CurrencyCode
					}, paymentMethod);
				}
				else
				{
					purchase = await Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{
						// 202 for payment success:
						toUpdate.Status = 202;
						toUpdate.PaymentGatewayInternalId = "";

					}, DataOptions.IgnorePermissions);
				}

				return new PurchaseAndAction()
				{
					Purchase = purchase
				};
			}

			// Get the gateway:
			gateway = _gateways.Get(purchase.PaymentGatewayId);

			if (gateway == null)
			{
				throw new PublicException(
					"The gateway providing your payment method is currently unavailable. If this keeps happening please let us know.",
					"gateway_unavailable"
				);
			}

			if (paymentMethod == null)
			{
				// Get the payment method:
				paymentMethod = await _paymentMethods.Get(context, purchase.PaymentMethodId);
			}

			// Ask the gateway to do the thing:
			return await gateway.ExecutePurchase(purchase, new ProductCost()
			{
				// Legal liability danger - do not set ExcludeTax to true unless you know what you are doing.
				Amount = toPay,
				AmountLessTax = purchase.TotalCostLessTax,
				CurrencyCode = purchase.CurrencyCode
			}, paymentMethod);
		}

		/// <summary>
		/// Get a unique user friendly reference for a purchase
		/// </summary>
		/// <returns></returns>
		public async ValueTask<string> GetReference(Context context)
		{
			var reference = RandomToken.Generate(16, 4, refPattern);

			// Is this slug unique?
			var existingPurchase = await Where("Reference=?", DataOptions.IgnorePermissions).Bind(reference).First(context);

			while (existingPurchase != null)
			{
				// Let's reroll and check again
				reference = RandomToken.Generate(16, 4, refPattern);
				existingPurchase = await Where("Reference=?", DataOptions.IgnorePermissions).Bind(reference).First(context);
			}
			return reference;
		}

		/// <summary>
		/// Requests the validation of a challenge response from the gateway
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="challengeResponse"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> ValidateChallenge(Context context, Purchase purchase, ChallengeResponse challengeResponse)
		{
			// Get the gateway:
			PaymentGateway gateway = _gateways.Get(purchase.PaymentGatewayId);

			if (gateway == null)
			{
				throw new PublicException(
					"The gateway providing your payment method is currently unavailable. If this keeps happening please let us know.",
					"gateway_unavailable"
				);
			}

			// Ask the gateway to do the thing:
			return await gateway.ValidateChallenge(purchase, challengeResponse);
		}

		/// <summary>
		/// Requests the validation of a hosted page payment transaction
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="hostedPageResponse"></param>
		/// <returns></returns>
		public async ValueTask<Purchase> ValidateHostedPayment(Context context,Purchase purchase, HostedPageResponse hostedPageResponse)
		{
			// Get the gateway:
			PaymentGateway gateway = _gateways.Get(purchase.PaymentGatewayId);

			if (gateway == null)
			{
				throw new PublicException(
					"The gateway providing your payment method is currently unavailable. If this keeps happening please let us know.",
					"purchase/gateway_unavailable"
				);
			}

			// Ask the gateway to do the thing:
			return await gateway.ValidateHostedPageTransaction(context, purchase, hostedPageResponse);
		}


	}
}
