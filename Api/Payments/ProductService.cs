using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Payments
{
	/// <summary>
	/// Handles products.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductService : AutoService<Product>
    {
		private ProductConfig _config;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductService() : base(Events.Product)
        {
			_config = GetConfig<ProductConfig>();

			InstallAdminPages("Products", "fa:fa-rocket", new string[] { "id", "name", "minQuantity" });

			Cache();
		}

		/// <summary>
		/// True if it should error if an order for less than the min is placed.
		/// Otherwise it will be rounded up.
		/// </summary>
		public bool ErrorIfBelowMinimum => _config.ErrorIfBelowMinimum;

		/// <summary>
		/// Gets the product tiers for a given product. The result is null if there are none.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<List<Product>> GetTiers(Context context, Product product)
		{
			if (product == null)
			{
				return null;
			}

			var tiers = await ListBySource(context, this, product.Id, "Tiers", DataOptions.IgnorePermissions);

			if (tiers != null && tiers.Count > 0)
			{
				// Tiers is not necessarily sorted, so:
				tiers.Sort((Product a, Product b) => {
					return a.MinQuantity.CompareTo(b.MinQuantity);
				});
			}

			return tiers;
		}
	}
    
}
