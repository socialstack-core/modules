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
	/// Handles shoppingCarts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ShoppingCartService : AutoService<ShoppingCart>
    {
		private ProductQuantityService _productQuantities;
		private PurchaseService _purchases;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ShoppingCartService(ProductQuantityService productQuantities, PurchaseService purchases) : base(Events.ShoppingCart)
        {
			_productQuantities = productQuantities;
			_purchases = purchases;
		}

		/// <summary>
		/// Checkout the cart using the given payment method. Intended to be called by actual user context.
		/// Returns a Purchase object which contains a clone of the objects in the cart.
		/// Will throw publicExceptions if the payment failed.
		/// You should however check the purchase.Status for immediate failures as well.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="payment">
		/// The payment method to use. If the user does not want to save their method this can just be a "new PaymentMethod()" in memory only instance.
		/// </param>
		public async ValueTask<Purchase> Checkout(Context context, ShoppingCart cart, PaymentMethod payment)
		{
			// Create a purchase (uses the user's locale from context):
			var purchase = await _purchases.Create(context, new Purchase() {
				ContentType = "ShoppingCart",
				ContentId = cart.Id,
				PaymentMethodId = payment.Id
			}, DataOptions.IgnorePermissions);

			// Copy the items from the shopping cart to the purchase.
			// This prevents any risk of someone manipulating their cart during the fulfilment.
			var inCart = await GetProducts(context, cart);
			await _purchases.AddProducts(context, purchase, inCart);

			// Attempt to fulfil the purchase now:
			return await _purchases.Execute(context, purchase, payment);
		}

		/// <summary>
		/// Gets the products in the given cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProducts(Context context, ShoppingCart cart)
		{
			return await _productQuantities.Where("ShoppingCartId=?", DataOptions.IgnorePermissions).Bind(cart.Id).ListAll(context);
		}

		/// <summary>
		/// Adds the given product to the cart.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cart"></param>
		/// <param name="product"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		public async ValueTask<ProductQuantity> AddToCart(Context context, ShoppingCart cart, Product product, uint quantity)
		{
			// Initial stock check. Note that a product can run out of stock whilst being in the cart.
			if (product.Stock != null)
			{
				if (quantity > product.Stock.Value)
				{
					throw new PublicException("Unfortunately there's not enough in stock to place your order.", "stock_insufficient");
				}
			}

			// Check if this product is already in this cart:
			var pQuantity = await _productQuantities
				.Where("ProductId=? and ShoppingCartId=?", DataOptions.IgnorePermissions)
				.Bind(product.Id)
				.Bind(cart.Id)
			.First(context);

			if (pQuantity == null)
			{
				// Create a new one:
				pQuantity = await _productQuantities.Create(context, new ProductQuantity()
				{
					ProductId = product.Id,
					ShoppingCartId = cart.Id,
					Quantity = quantity
				}, DataOptions.IgnorePermissions);
			}
			else
			{
				// Add to the existing one:
				await _productQuantities.Update(context, pQuantity, (Context ctx, ProductQuantity toUpdate, ProductQuantity orig) => {
					toUpdate.Quantity += quantity;
				});
			}

			return pQuantity;
		}

	}
    
}
