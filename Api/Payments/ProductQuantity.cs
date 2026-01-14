using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// Tracks a product and a quantity of the product.
	/// </summary>
	[ListAs("ProductQuantities")]
	[ImplicitFor("ProductQuantities", typeof(ShoppingCart))]
	[ImplicitFor("ProductQuantities", typeof(Subscription))]
	[ImplicitFor("ProductQuantities", typeof(Purchase))]

	// Used when an order is modified.
	[ListAs("RequestedProductQuantities", IsPrimary = false)]
	[ImplicitFor("RequestedProductQuantities", typeof(ShoppingCart))]
	[ImplicitFor("RequestedProductQuantities", typeof(Purchase))]

	[HasVirtualField("Product", typeof(Product), "ProductId")]

	public partial class ProductQuantity : VersionedContent<uint>
	{
		/// <summary>
		/// The product that this is a quantity of. The product may permit unlimited usage in which case units does not need to be used.
		/// </summary>
		public uint ProductId;
		
		/// <summary>
		/// The quantity.
		/// </summary>
		public ulong Quantity;

		/// <summary>
		/// The line item total (incl tax). Present only on Purchases.
		/// </summary>
		public ulong OrderedTotal;

		/// <summary>
		/// The line item total (excl tax). Present only on Purchases.
		/// </summary>
		public ulong OrderedTotalLessTax;

		/// <summary>
		/// The ordered currency code. Present only on Purchases.
		/// </summary>
		public string OrderedCurrencyCode;
	}

}