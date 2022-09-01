using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Emails;
using System;

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
		private SubscriptionService _subscriptions;
		private PriceService _prices;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseService(PaymentMethodService paymentMethods, PaymentGatewayService gateways, ProductQuantityService prodQuantities, ProductService products, EmailTemplateService emails, PriceService prices) : base(Events.Purchase)
        {
			_paymentMethods = paymentMethods;
			_gateways = gateways;
			_prodQuantities = prodQuantities;
			_products = products;
			_prices = prices;

			InstallEmails(
				new EmailTemplate()
				{
					Name = "Your payment receipt",
					Subject = "Your payment receipt",
					Key = "payment_receipt",
					BodyJson = "{\"t\":\"Email/Default\",\"c\":[{\"t\":\"Email/Centered\",\"d\":{}," +
					"\"c\":[\"Thank you! A payment was successfully made for \", {\"t\":\"UI/Token\",\"c\":\"${customData.printablePrice}\", \"d\":{\"mode\":\"customdata\",\"fields\":[\"printablePrice\"]}}]}," +
					"{\"t\":\"Email/PrimaryButton\",\"d\":{\"label\":\"View payment details\",\"target\":\"/checkout/payment/${customData.paymentId}\"}}]}"
				},
				new EmailTemplate()
				{
					Name = "A payment issue occurred",
					Subject = "A payment issue occurred",
					Key = "payment_fault",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":[\"We tried to request a payment of \", {\"t\":\"UI/Token\",\"c\":\"${customData.printablePrice}\", \"d\":{\"mode\":\"customdata\",\"fields\":[\"printablePrice\"]}}, \" but it was unable to go through. This can be because the card used was cancelled, has expired, or there are insufficient funds. If you're not sure, please check with your bank.\"]}," +
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"View payment details\",\"target\":\"/checkout/payment/${customData.paymentId}\"}}]}"
				}
			);
			
			Events.Purchase.BeforeCreate.AddEventListener(async (Context context, Purchase purchase) => {

				// Ensure paymentGatewayId is set:
				await EnsureGatewayId(context, purchase);

				// Ensure a locale is set:
				if (purchase.LocaleId == 0)
				{
					purchase.LocaleId = context.LocaleId;
				}

				return purchase;
			});

			Events.Purchase.BeforeUpdate.AddEventListener((Context context, Purchase purchase, Purchase original) => {

				// State change - is it now a successful payment?
				if (purchase.Status != original.Status)
				{
					if (purchase.Status == 202)
					{
						// Send success email:
						var userRecipient = new Recipient(purchase.UserId, purchase.LocaleId);
						userRecipient.CustomData = new
						{
							Purchase = purchase,
							PrintablePrice = PrintPrice(purchase.TotalCost, purchase.CurrencyCode),
							PaymentId = purchase.Id
						};
						var recipients = new List<Recipient>();
						recipients.Add(userRecipient);
						emails.Send(recipients, "payment_receipt");
					}
					else if (purchase.Status > 299)
					{
						// Send failure email:
						var userRecipient = new Recipient(purchase.UserId, purchase.LocaleId);
						userRecipient.CustomData = new
						{
							Purchase = purchase,
							PrintablePrice = PrintPrice(purchase.TotalCost, purchase.CurrencyCode),
							PaymentId = purchase.Id
						};
						var recipients = new List<Recipient>();
						recipients.Add(userRecipient);
						emails.Send(recipients, "payment_fault");
					}
				}

				return new ValueTask<Purchase>(purchase);

			});

		}

		private async ValueTask EnsureGatewayId(Context context, Purchase purchase)
		{
			if (purchase.PaymentMethodId == 0)
			{
				throw new PublicException("No payment gateway specified.", "payment_method_required");
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
		/// <param name="productQuantities"></param>
		/// <returns></returns>
		public async ValueTask AddProducts(Context context, Purchase toPurchase, List<ProductQuantity> productQuantities)
		{
			if (productQuantities == null || toPurchase == null)
			{
				return;
			}

			foreach (var pq in productQuantities)
			{
				var purchaseQuantity = new ProductQuantity()
				{
					ProductId = pq.ProductId,
					Quantity = pq.Quantity,
					PurchaseId = toPurchase.Id
				};

				// Inform that the given product quantity is being added to a purchase and is ready to be charged.
				// This is the place to inject usage based on reading some other dataset(s).
				purchaseQuantity = await Events.ProductQuantity.BeforeAddToPurchase.Dispatch(context, purchaseQuantity, toPurchase);

				if (purchaseQuantity == null)
				{
					continue;
				}

				// Add it:
				await _prodQuantities.Create(context, purchaseQuantity, DataOptions.IgnorePermissions);

			}
		}
		
		/// <summary>
		/// Gets the products in the given purchase.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProducts(Context context, Purchase purchase)
		{
			return await _prodQuantities.Where("PurchaseId=?", DataOptions.IgnorePermissions).Bind(purchase.Id).ListAll(context);
		}

		/// <summary>
		/// Calculates the total amount of the given purchase and returns it. Does not apply it to the purchase.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="coupon"></param>
		/// <returns></returns>
		public async ValueTask<ProductCost> CalcuateTotal(Context context, Purchase purchase, Coupon coupon = null)
		{
			// Get all product quantities:
			var productQuantities = await GetProducts(context, purchase);

			// Using the purchase locale, we'll now go through each product and establish the price
			// based on the number of units and the product unit price.
			// The product may be tiered though so we check for tiered prices as well.

			// Valid coupon?
			if (coupon != null)
			{
				if (coupon.Disabled || (coupon.ExpiryDateUtc.HasValue && coupon.ExpiryDateUtc.Value < System.DateTime.UtcNow))
				{
					// NB: If max number of people is reached, it is marked as disabled.
					throw new PublicException("Unfortunately the provided coupon has expired.", "coupon_expired");
				}
			}

			var hasSubscriptionProducts = false;
			string currencyCode = null;
			ulong totalCost = 0;

			foreach (var pq in productQuantities)
			{
				// Get the cost of this entry:
				var cost = await _prodQuantities.GetCostOf(pq, purchase.LocaleId);

				if (cost.SubscriptionProducts)
				{
					hasSubscriptionProducts = true;
				}

				if (currencyCode == null)
				{
					// First one:
					currencyCode = cost.CurrencyCode;
				}
				else
				{
					if (cost.CurrencyCode != currencyCode)
					{
						// Mixed currency purchases not supported.
						throw new PublicException("Unable to request a mixed currency purchase at this time.", "mixed_currencies");
					}
				}

				// Add to the cost:
				var prevTotal = totalCost;
				totalCost += cost.Amount;

				// Overflow checking:
				if (totalCost < prevTotal)
				{
					throw new PublicException("The requested quantity is too large.", "substantial_quantity");
				}

			}

			// Next, factor in the coupon.
			if (coupon != null)
			{
				var priceContext = context;

				if (purchase.LocaleId != context.LocaleId)
				{
					priceContext = new Context(purchase.LocaleId, 0, 0);
				}

				if (coupon.MinimumSpendAmount != 0)
				{
					// Get the relevant price:
					var minSpendPrice = await _prices.Get(priceContext, coupon.MinimumSpendAmount, DataOptions.IgnorePermissions);

					if (minSpendPrice != null)
					{
						// Are we above it?
						if (totalCost < minSpendPrice.Amount)
						{
							// No!
							throw new PublicException("Can't use this coupon as the total is below the minimum spend.", "min_spend");
						}
					}
				}

				if (coupon.DiscountPercent != 0)
				{
					var discountedTotal = totalCost * (1d - ((double)coupon.DiscountPercent / 100d));

					if (discountedTotal <= 0)
					{
						// Becoming free!
						totalCost = 0;
					}
					else
					{
						// Round to nearest pence/ cent
						totalCost = (ulong)Math.Ceiling(discountedTotal);
					}
				}

				if (coupon.DiscountFixedAmount != 0)
				{
					// Get the relevant price:
					var discountAmount = await _prices.Get(priceContext, coupon.DiscountFixedAmount, DataOptions.IgnorePermissions);

					if (discountAmount != null)
					{
						if (totalCost < discountAmount.Amount)
						{
							// Becoming free!
							totalCost = 0;
						}
						else
						{
							// Discount a fixed number of units:
							totalCost -= (ulong)discountAmount.Amount;
						}
					}
				}
			}

			return new ProductCost()
			{
				SubscriptionProducts = hasSubscriptionProducts,
				CurrencyCode = currencyCode,
				Amount = totalCost
			};
		}

		/// <summary>
		/// Exceutes potentially multiple subscriptions in one transaction. For example if someone wants to buy an annual and monthly subscription at the same time.
		/// This could also be whilst paying a one off amount too (in the provided purchase, which can be null).
		/// If a purchase is provided, the items from the subscription(s) will be copied to it and executed together.
		/// Otherwise, a purchase will be created and everything will be added to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="subscriptions"></param>
		/// <param name="paymentMethod"></param>
		/// <param name="coupon"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> MultiExecute(Context context, Purchase purchase, List<Subscription> subscriptions, PaymentMethod paymentMethod, Coupon coupon = null)
		{
			if (purchase == null)
			{
				// Create a purchase:
				purchase = await Create(context, new Purchase()
				{
					LocaleId = context.LocaleId,
					MultiExecute = true,
					PaymentGatewayId = paymentMethod.PaymentGatewayId,
					PaymentMethodId = paymentMethod.Id,
					UserId = context.UserId
				}, DataOptions.IgnorePermissions);
			}
			else
			{
				// Mark this as a multiExecute purchase
				if (!purchase.MultiExecute)
				{
					purchase = await Update(context, purchase, (Context c, Purchase p, Purchase orig) => {
						p.MultiExecute = true;
					}, DataOptions.IgnorePermissions);
				}
			}

			// Copy the items from the subs to the purchase:
			if (subscriptions != null)
			{
				if (_subscriptions == null)
				{
					_subscriptions = Services.Get<SubscriptionService>();
				}

				foreach (var subscription in subscriptions)
				{
					if (subscription == null)
					{
						continue;
					}

					// Add the sub to the multi-execute purchase:
					await CreateMappingIfNotExists(context, purchase.Id, _subscriptions, subscription.Id, "subscriptions");

					// Get its items, clone to purchase:
					var inSub = await _subscriptions.GetProducts(context, subscription);
					await AddProducts(context, purchase, inSub);
				}
			}

			// Attempt to fulfil the purchase now:
			return await Execute(context, purchase, paymentMethod, coupon);
		}
			
		/// <summary>
		/// Requests execution of the given payment. Internally calculates the total.
		/// This is triggered by the frontend after the given purchase has had a payment method attached to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="paymentMethod"></param>
		/// <param name="coupon"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> Execute(Context context, Purchase purchase, PaymentMethod paymentMethod = null, Coupon coupon = null)
		{
			// Event to indicate the purchase is about to execute:
			await Events.Purchase.BeforeExecute.Dispatch(context, purchase);

			// First ensure the correct total:
			var totalAmount = await CalcuateTotal(context, purchase, coupon);

			// If the total is free, we complete immediately, unless it's the first of a subscription payment.
			// If first subscription payment, must authorise the card.
			PaymentGateway gateway;

			if (totalAmount.Amount == 0)
			{
				if (totalAmount.SubscriptionProducts)
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

					if (totalAmount.CurrencyCode == null)
					{
						throw new PublicException(
							"Whoops! Sorry, we messed up. A currency code was missing from a free subscription purchase. It's required to make sure your bank knows what currency we'll be using. If this keeps happening, please let us know.",
							"currency_missing"
						);
					}

					if (paymentMethod == null)
					{
						// Get the payment method:
						paymentMethod = await _paymentMethods.Get(context, purchase.PaymentMethodId);
					}

					// Ask the gateway to authorise:
					return await gateway.AuthorisePurchase(purchase, totalAmount, paymentMethod, coupon);
				}
				else
				{
					purchase = await Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{

						// 202 for payment success:
						toUpdate.Status = 202;
						toUpdate.TotalCost = 0;
						toUpdate.CurrencyCode = null;
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
			return await gateway.ExecutePurchase(purchase, totalAmount, paymentMethod, coupon);
		}

	}
    
}
