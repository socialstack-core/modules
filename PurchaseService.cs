using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.PaymentGateways;
using Api.PurchaseProducts;

namespace Api.Purchases
{
	/// <summary>
	/// Handles purchases.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PurchaseService : AutoService<Purchase>
    {
		PurchaseProductService _purchaseProducts;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseService(PurchaseProductService purchaseProducts) : base(Events.Purchase)
        {
			_purchaseProducts = purchaseProducts;
		}

		/// <summary>
		/// Creates a purchase from a paymentIntent create request
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Purchase> CreatePurchase(Context context, PaymentIntentCreateRequest request, long cost, string currency)
        {
            var purchase = await Create(context, new Purchase { 
				UserId = context.UserId,
				TotalCostPence = cost,
				Currency = currency
			});

			foreach(var product in request.Products)
            {
				await _purchaseProducts.Create(context, new PurchaseProduct { PurchaseId = purchase.Id, ProductId = product.Id });
            }

			return purchase;
        }
	}
    
}
