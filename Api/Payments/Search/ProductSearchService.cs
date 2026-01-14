using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// Facet results.
/// </summary>
public class ProductSearchFacets
{
	/// <summary>
	/// Attribute facets.
	/// </summary>
	public List<AttributeValueFacet> Attributes;
	/// <summary>
	/// Category facets.
	/// </summary>
	public List<ProductCategoryFacet> Categories;
	/// <summary>
	/// Price range facets 
	/// </summary>
	public List<ProductPriceFacet> Prices;
}

/// <summary>
/// A facet value for a specific product category.
/// </summary>
[HasVirtualField("category", typeof(ProductCategory), "ProductCategoryId")]
public class ProductCategoryFacet
{
	/// <summary>
	/// The number of times this particular value appeared.
	/// </summary>
	public int Count;

	/// <summary>
	/// The category ID.
	/// </summary>
	public uint ProductCategoryId;
}

/// <summary>
/// A facet value for a specific product prices.
/// </summary>
public class ProductPriceFacet
{
	/// <summary>
	/// The price key (min/max etc).
	/// </summary>
	public string Key;

	/// <summary>
	/// The value
	/// </summary>
	public double? Value;
}


/// <summary>
/// A facet value for a specific product attribute.
/// </summary>
[HasVirtualField("value", typeof(ProductAttributeValue), "AttributeValueId")]
public class AttributeValueFacet
{
	/// <summary>
	/// The number of times this particular value appeared.
	/// </summary>
	public int Count;

	/// <summary>
	/// The ID for the value itself (e.g. "blue").
	/// </summary>
	public uint AttributeValueId;
}

/// <summary>
/// Searches for products.
/// </summary>
public class ProductSearch
{
	/// <summary>
	/// The textual query.
	/// </summary>
	public string Query;
	/// <summary>
	/// Page offset.
	/// </summary>
	public int PageIndex;
	/// <summary>
	/// The page size.
	/// </summary>
	public int PageSize;
	/// <summary>
	/// True if the search was handled.
	/// </summary>
	public bool Handled;
	/// <summary>
	/// Total result count.
	/// </summary>
	public int Total;
	/// <summary>
	/// The discovered products.
	/// </summary>
	public List<Product> Products;
	/// <summary>
	/// The set of applied facet filters. Can be null if none are active.
	/// </summary>
	public List<ProductSearchAppliedFacet> AppliedFacets;
	/// <summary>
	/// Facets in the results. Can be null for DBE's that don't support it.
	/// </summary>
	public ProductSearchFacets ResultFacets;

	/// <summary>
	/// Is this search reductive or expansive.
	/// </summary>
	public ProductSearchType SearchType;

	/// <summary>
	/// Include pricing stats with teh search results
	/// </summary>
	public bool IncludePriceStats;

	/// <summary>
	/// Include dynamic boosts related to product history and query
	/// </summary>
	public bool IncludeDynamicBoosts;

	/// <summary>
	/// Hide inactive products, so no sku, or variant children 
	/// </summary>
	public bool HideInactiveProducts;

	/// <summary>
	/// When in the admin panel allow search by id etc 
	/// </summary>
	public bool IsAdminPanel;

	/// <summary>
	/// Defaults to null, minimum price.
	/// </summary>
	public double? MinPrice = null;

	/// <summary>
	/// Defaults to null, maximum price.
	/// </summary>
	public double? MaxPrice = null;

	/// <summary>
	/// In stock only.
	/// </summary>
	public bool InStockOnly = false;

	/// <summary>
	/// Allow certain ids to be excluded, for example when adding to a order list 
	/// </summary>
	public List<uint> ExcludedIds = null;

	/// <summary>
	///  Allow custom parameters to control client specific functionality
	/// </summary>
	public Dictionary<string, object> CustomParameters = null;

	/// <summary>
	/// The sort order.
	/// </summary>
	public SortOrder SortOrder;
}

/// <summary>
/// A search facet for a particular mapping.
/// </summary>
public struct ProductSearchAppliedFacet
{
	/// <summary>
	/// The name of the mapping (e.g. "attributes").
	/// </summary>
	public string Mapping;

	/// <summary>
	/// The set of selected IDs. Should not be null if a filter is in use.
	/// </summary>
	public List<ulong> Ids;
}

/// <summary>
/// A particular facet result.
/// </summary>
public struct ProductSearchFacetResult
{
	/// <summary>
	/// E.g. the attribute value ID.
	/// </summary>
	public ulong EntityId;

	/// <summary>
	/// The number of results it has.
	/// </summary>
	public int Count;
}

/// <summary>
/// Handles product search with facets etc.
/// </summary>
public class ProductSearchService : AutoService
{
	private bool _searchFallback;

	private ProductSearchServiceConfig _productSearchConfig;

	private ProductCategoryService _categoryService;

	/// <summary>
	/// Instanced automatically.
	/// </summary>
	/// <param name="products"></param>
	/// <param name="catSvc"></param>
	public ProductSearchService(ProductService products, ProductCategoryService catSvc)
	{
		_productSearchConfig = GetConfig<ProductSearchServiceConfig>();
		_categoryService = catSvc;


		Events.Product.Search.AddEventListener(async (Context context, ProductSearch search) =>
		{

			if (search.Handled)
			{
				// No need to fallback.
				return search;
			}

			if (!_searchFallback)
			{
				_searchFallback = true;
				Log.Info("productsearch", "Using basic search fallback due to no dedicated engine mounting the search event.");
			}

			// This fallback does not support full facet info.
			var filter = products
				.Where("Name contains ? or Description contains ?")
				.Bind(search.Query)
				.Bind(search.Query);
			filter.SetPage(search.PageIndex, search.PageSize);
			var productSet = await filter.ListAll(context);

			search.Total = productSet.Count;
			search.Handled = true;
			search.Products = productSet;
			return search;
		}, 11);
	}

	/// <summary>
	/// Gets the current configuration. This is a live object - it updates whenever the config does.
	/// </summary>
	/// <returns></returns>
	public ProductSearchServiceConfig CurrentConfig()
	{
		return _productSearchConfig;
	}

	/// <summary>
	/// Searches products.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="query"></param>
	/// <param name="searchType"></param>
	/// <param name="sort"></param>
	/// <param name="inStockOnly"></param>
	/// <param name="includePriceStats"></param>
	/// <param name="includeDynamicBoosts"></param>
	/// <param name="hideInactiveProducts"></param>
	/// <param name="isAdminPanel"></param>
	/// <param name="appliedFacets">Facets to filter by (e.g. you want products which are colour 'red' via an attributes facet)</param>
	/// <param name="pageOffset"></param>
	/// <param name="pageSize"></param>
	/// <param name="minPrice"></param>
	/// <param name="maxPrice"></param>
	/// <param name="excludedIds"></param>
	/// <param name="customParameters"></param>
	/// <returns></returns>
	public async ValueTask<ProductSearch> Search(
		Context context, 
		string query, 
		ProductSearchType searchType, 
		SortOrder sort, 
		bool inStockOnly = false, 
		bool includePriceStats = false,
		bool includeDynamicBoosts = false, 
		bool hideInactiveProducts = false,
		bool isAdminPanel = false, 
		List<ProductSearchAppliedFacet> appliedFacets = null, 
		int pageOffset = 0, 
		int pageSize = 50, 
		double? minPrice = null, 
		double? maxPrice = null, 
		List<uint> excludedIds = null,
		Dictionary<string, object> customParameters = null)
	{
		if (pageOffset < 0)
		{
			pageOffset = 0;
		}

		if (pageSize < 1)
		{
			pageSize = 1;
		}

		if (pageSize > 100)
		{
			pageSize = 100;
		}

		var search = new ProductSearch()
		{
			Query = query,
			PageIndex = pageOffset,
			PageSize = pageSize,
			AppliedFacets = appliedFacets,
			SearchType = searchType,
			InStockOnly = inStockOnly,
			IncludePriceStats = includePriceStats,
			IncludeDynamicBoosts = includeDynamicBoosts,
			HideInactiveProducts = hideInactiveProducts,
			IsAdminPanel = isAdminPanel,
			ExcludedIds = excludedIds,
			CustomParameters = customParameters,
			SortOrder = sort,
			MinPrice = minPrice,
			MaxPrice = maxPrice
		};

		search = await Events.Product.Search.Dispatch(context, search);

		if (!search.Handled || search.Products == null)
		{
			return null;
		}

		if (search.ResultFacets == null)
		{
			search.ResultFacets = new ProductSearchFacets();
		}

		if (search.ResultFacets.Attributes == null && search.ResultFacets.Categories == null)
		{
			// Derive basic facets. You need atlas search on MongoDB (or another engine handling the Search event above)
			// to have a proper facet set. These are basic because the result is paginated, meaning facets for the vast majority of 
			// the results will be simply not factored in here.
			var categories = new Dictionary<ulong, int>();
			var attributes = new Dictionary<ulong, int>();

			foreach (var product in search.Products)
			{
				var cats = product.Mappings.Get("childofcategories");
				if (cats != null)
				{
					foreach (var catId in cats)
					{
						if (categories.TryGetValue(catId, out int currentCounter))
						{
							categories[catId] = currentCounter + 1;
						}
						else
						{
							categories[catId] = 1;
						}
					}
				}

				var attribs = product.Mappings.Get("attributes");

				if (attribs != null)
				{
					foreach (var attribId in attribs)
					{
						if (attributes.TryGetValue(attribId, out int currentCounter))
						{
							attributes[attribId] = currentCounter + 1;
						}
						else
						{
							attributes[attribId] = 1;
						}
					}
				}
			}

			search.ResultFacets.Attributes = ToAttributeFacets(attributes);
			search.ResultFacets.Categories = ToCategoryFacets(categories);
		}

		if (search.ResultFacets.Prices == null)
		{
			search.ResultFacets.Prices = new List<ProductPriceFacet>() {
						new ProductPriceFacet() { Key = "min" , Value = 0 },   // test values for UI
						new ProductPriceFacet() { Key = "max" , Value = 5000 } // test values for UI
					};
		}

		return search;
	}

	private List<ProductCategoryFacet> ToCategoryFacets(Dictionary<ulong, int> uniqueIds)
	{
		var vals = new List<ProductCategoryFacet>();

		foreach (var kvp in uniqueIds)
		{
			vals.Add(new ProductCategoryFacet()
			{
				ProductCategoryId = (uint)kvp.Key,
				Count = kvp.Value
			});
		}

		return vals;
	}

	private List<AttributeValueFacet> ToAttributeFacets(Dictionary<ulong, int> uniqueIds)
	{
		var vals = new List<AttributeValueFacet>();

		foreach (var kvp in uniqueIds)
		{
			vals.Add(new AttributeValueFacet()
			{
				AttributeValueId = (uint)kvp.Key,
				Count = kvp.Value
			});
		}

		return vals;
	}
}

/// <summary>
/// A struct representing the required payload for ordering a collection
/// </summary>
public struct SortOrder
{
	/// <summary>
	/// The field to order by
	/// </summary>
	public string Field;

	/// <summary>
	/// The sort direction
	/// </summary>
	public SortDirection Direction;
}

/// <summary>
/// The sort direction as an enum.
/// </summary>
public enum SortDirection
{
	/// <summary>
	/// Ascending
	/// </summary>
	ASC,
	/// <summary>
	/// Descending
	/// </summary>
	DESC
}