using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Products
{
	
	/// <summary>
	/// A Product
	/// </summary>
	public partial class Product : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the product
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Name;

		/// <summary>
        /// The description of the product
        /// </summary>
        [DatabaseField(Length = 500)]
		[Localized]
		public string Description;

		/// <summary>
		/// The id of this product in stripe (if intergrated with stripe)
		/// </summary>
		public string StripeProductId;

		/*
		 *	The below properties are depreciated
		 */

		/// <summary>
		/// [Depreciated - should now create 1 or more Price entities for each product]
		/// The cost of purchising this product in a one-off transaction in pence
		/// </summary>
		public long SingleCostPence;

		/// <summary>
		/// [Depreciated - should now create 1 or more Price entities for each product]
		/// The cost of purchising this product as a reccuring transaction in pence
		/// </summary>
		public long ReccuringCostPence;
	}

}