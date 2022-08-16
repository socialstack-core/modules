using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

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

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseService(PaymentMethodService paymentMethods, PaymentGatewayService gateways, ProductQuantityService prodQuantities, ProductService products) : base(Events.Purchase)
        {
			_paymentMethods = paymentMethods;
			_gateways = gateways;
			_prodQuantities = prodQuantities;
			_products = products;

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
		/// <returns></returns>
		public async ValueTask<ProductCost> CalcuateTotal(Context context, Purchase purchase)
		{
			// Get all product quantities:
			var productQuantities = await GetProducts(context, purchase);

			// Using the purchase locale, we'll now go through each product and establish the price
			// based on the number of units and the product unit price.
			// The product may be tiered though so we check for tiered prices as well.

			string currencyCode = null;
			ulong totalCost = 0;

			foreach (var pq in productQuantities)
			{
				// Get the cost of this entry:
				var cost = await _prodQuantities.GetCostOf(pq, purchase.LocaleId);

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

			return new ProductCost()
			{
				CurrencyCode = currencyCode,
				Amount = totalCost
			};
		}

		/// <summary>
		/// Requests execution of the given payment. Internally calculates the total.
		/// This is triggered by the frontend after the given purchase has had a payment method attached to it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="purchase"></param>
		/// <param name="paymentMethod"></param>
		/// <returns></returns>
		public async ValueTask<PurchaseAndAction> Execute(Context context, Purchase purchase, PaymentMethod paymentMethod = null)
		{
			// Event to indicate the product is about to execute:
			await Events.Purchase.BeforeExecute.Dispatch(context, purchase);

			// First ensure the correct total:
			var totalAmount = await CalcuateTotal(context, purchase);

			// If the total is free, we complete immediately.
			if (totalAmount.Amount == 0)
			{
				purchase = await Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) => {

					// 202 for payment success:
					toUpdate.Status = 202;
					toUpdate.TotalCost = 0;
					toUpdate.CurrencyCode = null;
					toUpdate.PaymentGatewayInternalId = "";

				}, DataOptions.IgnorePermissions);

				return new PurchaseAndAction()
				{
					Purchase = purchase
				};
			}

			// Get the gateway:
			var gateway = _gateways.Get(purchase.PaymentGatewayId);

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
			return await gateway.ExecutePurchase(purchase, totalAmount, paymentMethod);
		}

	}
    
}
