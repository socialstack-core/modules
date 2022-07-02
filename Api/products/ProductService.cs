using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.PaymentGateways;
using Stripe;
using Api.CanvasRenderer;

namespace Api.Products
{
	/// <summary>
	/// Handles products.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductService : AutoService<Product>
    {
		PaymentGatewayConfig _paymentGatewayConfig;
		FrontendCodeService _frontEndCode;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductService(FrontendCodeService frontEndCode) : base(Eventing.Events.Product)
        {
			InstallAdminPages("Products", "fa:fa-cubes", new string[] { "id", "name", "singleCostPence" });

			_frontEndCode = frontEndCode;
			_paymentGatewayConfig = GetConfig<PaymentGatewayConfig>();

			Eventing.Events.Product.AfterCreate.AddEventListener(async (Context ctx, Product product) => {

				if (product == null)
				{
					return product;
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					var stripeProduct = await CreateStripeProduct(ctx, product, stripeSecret);

					// update product and price with Ids from stripe
					var productToUpdate = await StartUpdate(ctx, product, DataOptions.IgnorePermissions);

					if (productToUpdate != null)
					{
						productToUpdate.StripeProductId = stripeProduct.Id;

						product = await FinishUpdate(ctx, productToUpdate, product);
					}
				}

				return product;
			});

			Eventing.Events.Product.BeforeUpdate.AddEventListener(async (Context ctx, Product product, Product original) => {

				if (product == null)
				{
					return product;
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					if (!string.IsNullOrEmpty(product.StripeProductId))
                    {
						var productOptions = new ProductUpdateOptions
						{
							Name = product.Name,
							Description = product.Description
						};

						var stripeProducts = new Stripe.ProductService();
						var stripeProduct = await stripeProducts.UpdateAsync(product.StripeProductId, productOptions);
					}
					else
                    {
						var stripeProduct = await CreateStripeProduct(ctx, product, stripeSecret);

						product.StripeProductId = stripeProduct.Id;
					}
				}

				return product;
			});

			Eventing.Events.Product.AfterDelete.AddEventListener(async (Context ctx, Product product) => {

				if (product == null)
				{
					return product;
				}

				var stripeSecret = _paymentGatewayConfig.StripeSecretKey;

				// If we are intergrated with stripe
				if (!string.IsNullOrEmpty(stripeSecret))
				{
					if (!string.IsNullOrEmpty(product.StripeProductId))
					{
						StripeConfiguration.ApiKey = stripeSecret;

						var productOptions = new ProductUpdateOptions
						{
							Active = false
						};

						var stripeProducts = new Stripe.ProductService();
						var stripeProduct = await stripeProducts.UpdateAsync(product.StripeProductId, productOptions);
					}
				}

				return product;
			});
		}

		/// <summary>
		/// Creates a product in stripe for the given local product
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="product"></param>
		/// <param name="stripeSecret"></param>
		/// <returns></returns>
		public async Task<Stripe.Product> CreateStripeProduct(Context ctx, Product product, string stripeSecret)
		{
			//create stripe product and price
			StripeConfiguration.ApiKey = stripeSecret;
			var hostname = _frontEndCode.GetPublicUrl().Replace("https://", "").Replace("http://", "");

			var metadata = new Dictionary<string, string> {
						{ "Product Id", product.Id.ToString() },
						{ "Parent App Hostname" , hostname }
					};

			var productOptions = new ProductCreateOptions
			{
				Name = product.Name,
				Description = product.Description,
				Metadata = metadata
			};

			var stripeProducts = new Stripe.ProductService();
			var stripeProduct = await stripeProducts.CreateAsync(productOptions);

			return stripeProduct;
		}
	}
    
}
