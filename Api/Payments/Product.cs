using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Payments
{

	/// <summary>
	/// A Product. Price is specified via price tiers as a product can have bulk discounts.
	/// Typically though you would include calculatedPrice as that resolves both customer 
	/// specific pricing and provides with/without tax results.
	/// </summary>

	[ListAs("OptionalExtras", IsPrimary = false)]
	[ImplicitFor("OptionalExtras", typeof(Product))]
	[ImplicitFor("OptionalExtras", typeof(ProductTemplate))]

	[ListAs("Accessories", IsPrimary = false)]
	[ImplicitFor("Accessories", typeof(Product))]
	[ImplicitFor("Accessories", typeof(ProductTemplate))]

	[ListAs("Suggestions", IsPrimary = false)]
	[ImplicitFor("Suggestions", typeof(Product))]
	[ImplicitFor("Suggestions", typeof(ProductTemplate))]

	[ListAs("Variants", IsPrimary = false)]
	[ImplicitFor("Variants", typeof(Product))]
	[ImplicitFor("Variants", typeof(ProductTemplate))]

	[HasVirtualField("primaryCategory", typeof(ProductCategory), "PrimaryCategoryId")]
	[HasVirtualField("productTemplate", typeof(ProductTemplate), "ProductTemplateId")]

	[HasSecondaryResult("attributeValueFacets", typeof(AttributeValueFacet))]
	[HasSecondaryResult("productCategoryFacets", typeof(ProductCategoryFacet))]
	public partial class Product : ProductBase
	{
		/// <summary>
		/// The unique identifier for product
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Sku;

		/// <summary>
		/// Used by atlas search to show score (mostly to debug)
		/// </summary>
		[ComputedSearch]
		public double Score { get; set; }

		/// <summary>
		/// Used by search to sort by most popular (sitewide)
		/// </summary>
		public double Popularity;

		/// <summary>
		/// A template that this product was created from.
		/// Used for required attributes (alongside the product categories).
		/// </summary>
		public uint ProductTemplateId;

	}

	/// <summary>
	/// Product fields shared with product templates as well. Don't put the virtual fields on this.
	/// </summary>
	public partial class ProductBase : VersionedContent<uint>
	{
		/// <summary>
		/// 0 = Physical
		/// 1 = Digital (no delivery method required)
		/// 2 = Variant (you can specify variations on this product, and a template for the variations is recommended).
		/// </summary>
		[Module("Admin/Payments/ProductTypes")]
		[Data("sortOrder", "1")]
		public uint ProductType;

		/// <summary>
		/// The name of the product
		/// </summary>
		[DatabaseField(Length = 200)]
		[Data("required", "true")]
		[Data("validate", "Required")]
		[Meta("title")]
		public Localized<string> Name;

		/// <summary>
		/// The slug for product
		/// </summary>
		[DatabaseField(Length = 1000)]
		[Data("readonly", true)]
		[DatabaseIndex(Unique=true, Scope ="database")]
		public string Slug;

		/// <summary>
		/// In the atomic currency unit (pence), the nominal value of free samples used for tax purposes.
		/// This must be set if the configured price is zero. It is not triggered in the event 
		/// that an order's value is discounted to zero through coupons or other promotions.
		/// </summary>
		public uint? FreeSampleNominalValue;

        /// <summary>
		/// True if this product is billed by usage.
		/// </summary>
		[Data("help", "Tick this if this product is billed after it has been used based on the amount of usage it has had.")]
		public bool IsBilledByUsage;

		/// <summary>
		/// True if this product is featured and should be boosted in search etc 
		/// </summary>
		[Data("help", "Tick this if this product is featured and should be highlighted on the site")]
		public bool IsFeatured;

		/// <summary>
		/// Used to indicate if this product recurs and if so, the frequency.
		/// 0 = One off
		/// 1 = Weekly
		/// 2 = Monthly
		/// 3 = Quarterly
		/// 4 = Yearly
		/// </summary>
		[Module("Admin/Payments/BillingFrequencies")]
		public uint BillingFrequency;

		/// <summary>
		/// Used to indicate if this product is tax exempt
		/// 0 = No
		/// 1 = Yes
		/// 2 = Eligibility required
		/// </summary>
		[Module("Admin/Payments/TaxExempt")]
		[Data("help", "Identify if the product is tax exempt")]
		public uint TaxExempt;

		/// <summary>
		/// Used to indicate the availability of the product
		/// 0 = Yes
		/// 1 = Pre order
		/// 2 = No (permanently)
		/// 3 = No (awaiting stock)
		/// </summary>
		[Module("Admin/Payments/Availability")]
		[Data("help", "Identify if the product is currently available")]
		public uint Availability;

		/// <summary>
		/// The description of this product.
		/// </summary>
		public Localized<string> DescriptionHtml;

		/// <summary>
		/// The raw metadata/content of this product, used for free text search
		/// </summary>
		[Data("readonly", true)]
		[JsonIgnore]
		public string DescriptionRaw;

		/// <summary>
		/// Text description of how many of a product are sold as a unit eg. EACH, pack, 6 etc.
		/// </summary>
		public string SellUnitDescription;

		/// <summary>
		/// The feature image ref
		/// </summary>
		[DatabaseField(Length = 300)]
		[Meta("image")]
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
		/// Available stock. Null indicates it is unlimited.
		/// </summary>
		public uint? Stock;

		/// <summary>
		/// Indicates if this is a variant product related to a parent base product
		/// </summary>
		[Data("tab", "linkToParent")]
		[Data("label", "Variant Of")]
		[Module("Admin/ContentSelect")]
		[Data("contentType", "Product")]
		[Data("search", "name")]
		public uint? VariantOfId;

		/// <summary>
		/// Continue selling when there is no stock
		/// usually useful for products that can be
		/// back-ordered. It's false by default.
		/// </summary>
		[Data("help", "Can this continue to be fulfilled even when there is no physical stock?")]
		public bool ContinueSellingWithNoStock = false;

		/// <summary>
		/// The ID of the primary product category. This is just a convenience field for 
		/// being the equiv of the first mapping entry, and exists to make it easily includable.
		/// </summary>
		[JsonIgnore]
		public uint PrimaryCategoryId => (uint)Mappings.GetFirst("ProductCategories");

		/// <summary>
		/// JsonString containing the default price tiers for this product
		/// </summary>
		[Module("Admin/Payments/PriceTable")]
		public JsonString PriceTiersJson;

		private List<Price> _priceTiers;


		/// <summary>
		/// The set of temporary variants which may exist during a creation or update only.
		/// They are used to update (or create) the real product that they reference.
		/// </summary>
		private List<PartialProductVariant> _tempVariants { get; set; }

		/// <summary>
		/// Gets the set of temporary variants which may exist during a creation or update only.
		/// </summary>
		/// <returns></returns>
		public List<PartialProductVariant> GetTemporaryVariants()=> _tempVariants;

		/// <summary>
		/// Sets the set of temporary variants which may exist during a creation or update only.
		/// </summary>
		/// <param name="tempVariants"></param>
		public void SetTemporaryVariants(List<PartialProductVariant> tempVariants)
		{
			_tempVariants = tempVariants;
		}

		/// <summary>
		/// Adds a temporary 
		/// </summary>
		/// <param name="variant"></param>
		public void AddTemporaryVariantInfo(PartialProductVariant variant)
		{
			// Note that this doesn't encounter threading issues as the product
			// instance this is ocurring on is not cache shared.
			if (_tempVariants == null)
			{
				_tempVariants = new List<PartialProductVariant>();
			}
			_tempVariants.Add(variant);
		}

		/// <summary>
		/// The set of temporary components which may exist during a creation or update only.
		/// </summary>
		private List<PartialProductComponent> _tempComponents { get; set; }

		/// <summary>
		/// Gets the set of temporary components which may exist during a creation or update only.
		/// </summary>
		/// <returns></returns>
		public List<PartialProductComponent> GetTemporaryComponents() => _tempComponents;

		/// <summary>
		/// Sets the set of temporary components which may exist during a creation or update only.
		/// </summary>
		/// <param name="tempComponents"></param>
		public void SetTemporaryComponents(List<PartialProductComponent> tempComponents)
		{
			_tempComponents = tempComponents;
		}

		/// <summary>
		/// Adds a temporary 
		/// </summary>
		/// <param name="component"></param>
		public void AddTemporaryComponentInfo(PartialProductComponent component)
		{
			// Note that this doesn't encounter threading issues as the product
			// instance this is ocurring on is not cache shared.
			if (_tempComponents == null)
			{
				_tempComponents = new List<PartialProductComponent>();
			}
			_tempComponents.Add(component);
		}

		/// <summary>
		/// The list of default prices for this product extracted from PriceTiers
		/// </summary>
		[JsonIgnore]
		public List<Price> PriceTiers
		{
			get
			{
				if (_priceTiers == null && PriceTiersJson.HasValue)
				{
					_priceTiers = JsonConvert.DeserializeObject<List<Price>>(PriceTiersJson.ToString());
				}
				return _priceTiers;
			}
			set => _priceTiers = value;
		}

		/// <summary>
		/// Update the PriceTiers json for this product.
		/// </summary>
		public void UpdatePricesJson()
		{
			if (_priceTiers == null)
			{
				return;
			}

			//Make sure the order is correct before saving
			SortPrices();

			PriceTiersJson = new JsonString(JsonConvert.SerializeObject(_priceTiers));

			//reset _prices in the cache
			_priceTiers = null;
		}

		/// <summary>
		/// Sort the prices for based on minimum quantity
		/// </summary>
		private void SortPrices()
		{
			PriceTiers.Sort((a, b) => a.MinimumQuantity.CompareTo(b.MinimumQuantity));
		}
	}

}
