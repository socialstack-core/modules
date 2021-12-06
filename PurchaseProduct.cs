using System;
using Api.Database;
using Api.Products;
using Api.Purchases;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.PurchaseProducts
{
	
	/// <summary>
	/// A PurchaseProduct
	/// </summary>
	[HasVirtualField("Purchase", typeof(Purchase), "PurchaseId")]
	[HasVirtualField("Product", typeof(Product), "ProductId")]
	public partial class PurchaseProduct : Content<uint>
	{
		/// <summary>
		/// The Purchase the product belongs to
		/// </summary>
		public uint PurchaseId;

		/// <summary>
		/// The Product being purchased
		/// </summary>
		public uint ProductId;
	}

}