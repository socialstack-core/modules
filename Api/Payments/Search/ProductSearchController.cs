using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>Handles product search endpoints.</summary>
[Route("v1/product/search")]
public partial class ProductSearchController : AutoController
{
	ProductSearchService _productSearchService;
	ProductService _productService;

	/// <summary>
	/// Instanced automatically.
	/// </summary>
	/// <param name="productSearchService"></param>
	/// <param name="productService"></param>
	public ProductSearchController(ProductSearchService productSearchService, ProductService productService)
	{
		_productSearchService = productSearchService;
		_productService = productService;
	}

	/// <summary>
	/// Faceted product search. You can provide text and optionally facets.
	/// It will then return the product set along with facets to display.
	/// Note that Atlas search is required for this to be full-featured (full facet sets)
	/// but you will still see a functional subset otherwise.
	/// </summary>
	/// <returns></returns>
	[HttpGet("faceted")]
	public ValueTask<ContentStream<Product, uint>?> GetFaceted(Context context, [FromQuery] string query)
	{
		return Faceted(context, new ProductSearchRequest()
		{
			Query = query
		});
	}

	/// <summary>
	/// Faceted product search. You can provide text and optionally facets.
	/// It will then return the product set along with facets to display.
	/// Note that Atlas search is required for this to be full-featured (full facet sets)
	/// but you will still see a functional subset otherwise.
	/// </summary>
	/// <returns></returns>
	[HttpPost("faceted")]
	public async ValueTask<ContentStream<Product, uint>?> Faceted(Context context, [FromBody] ProductSearchRequest request)
	{
		var resultSet = await _productSearchService.Search(
			context, 
			request.Query, 
			request.SearchType,
			request.SortOrder, 
			request.InStockOnly, 
			request.IncludePriceStats,
			request.IncludeDynamicBoosts,
			request.HideInactiveProducts,
			request.IsAdminPanel,
			request.AppliedFacets, 
			request.PageOffset,
			(int) request.PageSize,
			request.MinPrice, 
			request.MaxPrice,
			request.ExcludedIds,
			request.CustomParameters
		);

		if (resultSet == null)
		{
			return null;
		}

		// In order to support includes and help out the SSR on this endpoint
		// we need to be returning a ContentStream. Because we are also returning facet sets
		// we thus need to use a mutli-stream: the primary result set is the list of products
		// and the secondary ones are any of the facets.
		// This results in json of the form:
		// {
		//     "results": [product, product, ..],
		//     "includes": [any includes for the products],
		//     "secondary": {
		//       "attributeValueFacets": {
		//           "results": [attrFacet, attrFacet, ..],
		//           "includes": [any includes for attribute facets]
		//       },
		//       "productCategoryFacets": {
		//           "results": [catFacet, catFacet, ..],
		//           "includes": [any includes for category facets]
		//       }
		//     }
		// }
		
		// These are declared on Product as HasSecondaryResult, which isn't mandatory but does make them support includes as well.
		// This means you can include the attributeValue,category and attribute plus anything else you might want
		// even though these AttributeValueFacet instances are not actually regular content types.
		var secondarySources = new SecondaryListStreamSource<AttributeValueFacet>(
			"attributeValueFacets", 
			resultSet.ResultFacets.Attributes
		);

		var secondarySourceNext = secondarySources.Next = new SecondaryListStreamSource<ProductCategoryFacet>(
			"productCategoryFacets",
			resultSet.ResultFacets.Categories
		);

		secondarySourceNext.Next = new SecondaryListStreamSource<ProductPriceFacet>(
			"productPriceFacets",
			resultSet.ResultFacets.Prices
		);

		return new ContentStream<Product, uint>()
		{
			ServiceForType = _productService,
			Source = new MultiStreamSource<Product, uint>(
				new ProductListStreamSource<Product, uint>(
					resultSet
				),
				secondarySources
			),
			IncludeTotal = true
		};
	}
	
}

/// <summary>
/// A search result.
/// </summary>
public struct ProductSearchResult
{
	/// <summary>
	/// The current page of products.
	/// </summary>
	public List<Product> Products;

	/// <summary>
	/// Total result count.
	/// </summary>
	public int Total;

	/// <summary>
	/// Facets present on this result set.
	/// </summary>
	public ProductSearchFacets Facets;
}

/// <summary>
/// A search request.
/// </summary>
public class ProductSearchRequest
{
	/// <summary>
	/// Page offset.
	/// </summary>
	[JsonOptions(Optional = true)]
	public int PageOffset;

	/// <summary>
	/// The search text itself.
	/// </summary>
	public string Query;

	/// <summary>
	/// Optional applied facets per mapping (e.g. you want to filter results by attributes containing the colour 'blue').
	/// </summary>
	[JsonOptions(Optional = true)]
	public List<ProductSearchAppliedFacet> AppliedFacets;

	/// <summary>
	/// The max result set
	/// </summary>
	[JsonOptions(Optional = true)]
	public uint PageSize;

	/// <summary>
	/// Is it a reductive or an expansive search.
	/// </summary>
	[JsonOptions(Optional = true)]
	public ProductSearchType SearchType;

	/// <summary>
	/// Include pricing stats with teh search results
	/// </summary>
	[JsonOptions(Optional = true)]
	public bool IncludePriceStats;

	/// <summary>
	/// Include dynamic boosts related to product history and query
	/// </summary>
	[JsonOptions(Optional = true)]
	public bool IncludeDynamicBoosts;

	/// <summary>
	/// Hide inactive products, so no sku, or variant children 
	/// </summary>
	[JsonOptions(Optional = true)]
	public bool HideInactiveProducts;

	/// <summary>
	/// When in the admin panel allow search by id etc 
	/// </summary>
	[JsonOptions(Optional = true)]
	public bool IsAdminPanel;

	/// <summary>
	/// Minimum price.
	/// </summary>
	[JsonOptions(Optional = true)]
	public double? MinPrice = null;

	/// <summary>
	/// Maximum price
	/// </summary>
	[JsonOptions(Optional = true)]
	public double? MaxPrice = null;

	/// <summary>
	/// In stock only.
	/// </summary>
	[JsonOptions(Optional = true)]
	public bool InStockOnly = false;

	/// <summary>
	/// Defined sort order to change the order in which the result set is
	/// displayed.
	/// </summary>
	[JsonOptions(Optional = true)]
	public SortOrder SortOrder;

	/// <summary>
	/// Allow certain ids to be excluded, for example when adding to a order list 
	/// </summary>
	[JsonOptions(Optional = true)]
	public List<uint> ExcludedIds = null;

	/// <summary>
	///  Allow custom parameters to control client specific functionality
	/// </summary>
	[JsonOptions(Optional = true)]
	public Dictionary<string, object> CustomParameters = null;
}


/// <summary>
/// An enum to set whether the query is reductive or expansive. 
/// </summary>
public enum ProductSearchType
{
	/// <summary>
	/// Used to narrow down a target product, works how you'd expect on an admin panel.
	/// I want products that only have...
	/// </summary>
	Reductive,
	/// <summary>
	/// Used to broaden the net cast to the product pool, by saying I want products that have any of....
	/// </summary>
	Expansive
}