using System;
using Api.Database;
using Api.Translate;
using Api.Products;
using Api.Startup;
using Api.Users;


namespace Api.Discounts
{

	/// <summary>
	/// A Discount
	/// </summary>
	[HasVirtualField("Product", typeof(Product), "ProductId")]
	public partial class Discount : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the discount.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Name;

		/// <summary>
		/// The percentage coupon discount (1-100).
		/// </summary>
		public uint DiscountPercentage;

		/// <summary>
		/// The value of the discount in pence.
		/// </summary>
		public ulong DiscountPence;

		/// <summary>
		/// The specific product this discount applies to (if any).
		/// </summary>
		public uint ProductId;

		/// <summary>
		/// Is this discount disabled?
		/// </summary>
		public bool IsDisabled;
	}

}