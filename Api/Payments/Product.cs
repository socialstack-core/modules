using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Product
	/// </summary>
	[ListAs("Tiers")]
	[ImplicitFor("Tiers", typeof(Product))]
	[HasVirtualField("Price", typeof(Price), "PriceId")]
	public partial class Product : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the product
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Name;

		/// <summary>
		/// Usually used by tiered products. This is the minimum purchase quantity of this product.
		/// </summary>
		public ulong MinQuantity;

		/// <summary>
		/// The content of this product.
		/// </summary>
		[Localized]
		public string DescriptionJson;

		/// <summary>
		/// The feature image ref
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// Indicates how the price is computed based on the quantity of the product. See "Pricing strategy" on the wiki for more info.
		/// 0 = Standard strategy. Select tier (or base product) based on minQuantity, then perform quantity * tier.
		/// 1 = Step once strategy. maxQuantity * tierPrice for base tier, then standard.
		/// 2 = Step always. maxQuantity * tierPrice per tier always.
		/// </summary>
		[Module("Admin/Payments/PriceStrategies")]
		[Data("help", "If you're using tiers, this defines the calculation used for the final price.")]
		public uint PriceStrategy;

		/// <summary>
		/// The price to use.
		/// </summary>
		[Localized]
		public uint PriceId;

		/// <summary>
		/// Available stock. Null indicates it is unlimited.
		/// </summary>
		public uint? Stock;
	}

}