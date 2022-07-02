using System;
using Api.Database;
using Api.Products;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Prices
{
	/// <summary>
	/// A Price
	/// </summary>
	[HasVirtualField("Product", typeof(Product), "ProductId")]
	public partial class Price : VersionedContent<uint>
	{
		/// <summary>
		/// The nickname of the price
		/// </summary>
		public string Name;

		/// <summary>
		/// The id of this product
		/// </summary>
		public uint ProductId;

		/// <summary>
		/// The id of this price in stripe (if intergrated with stripe)
		/// </summary>
		public string StripePriceId;

		/// <summary>
		/// The cost of purchasing this product
		/// </summary>
		[Localized]
		public long CostPence;

		/// <summary>
		/// Is this price recurring?
		/// </summary>
		public bool IsRecurring;

		/// <summary>
		/// Is this price metered I.E. does the price depend upon usage?
		/// </summary>
		public bool IsMetered;

		/// <summary>
		/// How many months should pass between payments?
		/// </summary>
		public uint RecurringPaymentIntervalMonths;

	}

}