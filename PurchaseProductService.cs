using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PurchaseProducts
{
	/// <summary>
	/// Handles purchaseProducts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PurchaseProductService : AutoService<PurchaseProduct>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseProductService() : base(Events.PurchaseProduct)
        {
			// Example admin page install:
			// InstallAdminPages("PurchaseProducts", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
