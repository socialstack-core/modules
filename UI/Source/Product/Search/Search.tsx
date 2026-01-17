import {useState} from "react";
import {ProductCategory} from "Api/ProductCategory";
import CategoryFilters from 'UI/ProductCategory/Filters';
import ProductList from 'UI/Product/List';
import Loading from 'UI/Loading';
import productApi from 'Api/Product';
import useApi from "UI/Functions/UseApi";
import Html from 'UI/Html';
import Input from 'UI/Input';
import DualRange from 'UI/DualRange';
import { useSession } from 'UI/Session';
import searchApi, {ProductSearchAppliedFacet, ProductSearchType, SortDirection} from "Api/ProductSearchController";
import {ProductAttributeValue } from "Api/ProductAttributeValue";
import {useRouter} from "UI/Router";
import { AttributeFacetGroup, AttributeValueFacet, ProductCategoryFacet, ProductPriceFacet} from "UI/Product/Search/Facets";
import FilterList, {CategoryFilterList} from "UI/Product/Search/FilterList";
import Breadcrumb from "UI/Breadcrumb";
import Button from "UI/Button";
import Popover from "UI/Popover";
import LoadMore  from "UI/LoadMore";
import LazyLoader  from "UI/LazyLoader";
import {SearchCriteria, useRouterCriteria } from "./UseRouterCriteria";
import store from 'UI/Functions/Store';
import Link from "UI/Link";
import {ProductCardAfterItem} from "../types";

const MAX_VISIBLE_CATEGORIES = 3;
const ROOT_CATEGORY_ID: uint = 1 as uint;

/**
 * Props for the Search component.
 */
interface SearchProps {
	// Connected via a graph in the page, which is also where the includes are defined.
	// This component requires at least the following includes:
	// productCategories, productCategories.primaryUrl
	lazyLoad: boolean,
	allowReset: boolean,
    showDebug: boolean,
	productCategory?: ProductCategory,
	customParameters: Record<string,any>,
	
	// ==================================================
	// Product Item After
	// --------------------------------------------------
	// This has been added to allow content to be
	// injected at the end of the product card
	// component, this prop exists as 2 possible 
	// types, a string, which is just the path to 
	// the component. This is also an array, so multiple
	// items can be added without resorting to hacky
	// injection.
	// --- EXAMPLE --------------------------------------
	// UI/Products/StockLevel
	// --------------------------------------------------
	// or an actual component, the component 
	// passed in this prop must take a current 
	// product in the props. It will be called
	// --- JS -------------------------------------------
	// <Component product={product}/>
	// --------------------------------------------------
	productItemAfter?: ProductCardAfterItem[],
	// ==================================================
}

type SecondaryIncludes = {
	attributeValueFacets?: {
		results: AttributeValueFacet,
	}
	productCategoryFacets?: {
		results: ProductCategoryFacet[]
	}
	productPriceFacets? : {
		results: ProductPriceFacet
	}
}

type PriceRange = {
    min?:number,
    max?:number
}

/**
 * The Search React component.
 * @param props React props.
 */
const Search: React.FC<SearchProps> = (props) => {
	const { 
		productCategory ,
		customParameters,
		lazyLoad = true,
		allowReset = true,
        showDebug = false,       
    } = props;

	const { session } = useSession();
	var { role, business } = session;
	
	const { pageState , updateQuery, removeQueryItems } = useRouter();
	
	// ============================================
	// Additional Component: Init
	// --------------------------------------------
	// if the item is a component, then map
	// the initial value back, nothing really
	// needs to happen with this. When it's 
	// a string; then we need to use the
	// global "require" method to grab the 
	// components definition ready for invocation 
	const additionalComponents = props?.productItemAfter?.map(item => {
		if (typeof item !== 'string') {
			return item;
		}
		
		return require(item)?.default ?? null;
	}) ?? [] // or default to an empty array.
	// ============================================

	// manage filters via query string
	const { criteria, hasCriteria, setCriteria, removeCriteria, clearAllCriteria } = useRouterCriteria({
		pageState, // from router
		updateQuery, // from router
		removeQueryItems, // from router
		defaults: {},
		onChange: runSearch,
		// When 'q' changes anywhere, reset pagination and clear filters etc
		resetOnQChange: { page: undefined, min: undefined, max: undefined, facets: undefined },
		skipResetOnFirstLoad: true
	});
   
	const [minPrice, setMinPrice] = useState<double>(null);
	const [maxPrice, setMaxPrice] = useState<double>(null);

    // price range, will get set by results
    const [lowestPrice, setLowestPrice] = useState<double>(null);
	const [highestPrice, setHighestPrice] = useState<double>(null);

	const [categorySearch, setCategorySearch] = useState('');

	// keep track for lazy loading
	const [currentPage, setCurrentPage] = useState(1);
	const [currentCriteria, setCurrentCriteria] = useState<SearchCriteria>({});
	const [hasMore, setHasMore] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    const [products, setProducts] = useApi(async () => {
		if (window.SERVER) {
			console.log('initial load');
			await loadProducts();
		}
    },[])   

	const handleUpdatedFacets = (updatedFacets:ProductAttributeValue[]) => {
		const uniqueAttributes: Record<number, number[]> = {};

		if (updatedFacets.length > 0) {
			for (const attribValue of updatedFacets) {
				const attributeId = attribValue.productAttributeId;

				if (!uniqueAttributes[attributeId]) {
					uniqueAttributes[attributeId] = [];
				}

				uniqueAttributes[attributeId].push(attribValue.id);
			}
		}

		// console.log('mapped facets',uniqueAttributes);
		setCriteria({ facets: Object.keys(uniqueAttributes).length !== 0 ? uniqueAttributes : {}, page: undefined })
	};

	const handleLoadMore = async () => {
        // console.log('lazy loading');
        await loadProducts({...currentCriteria, page:currentPage + 1});
	};

    async function runSearch(c: SearchCriteria) {
        // console.log("Searching with:", c);
        await loadProducts(c);
    }

	const loadProducts = async (criteria: SearchCriteria = {}) => {
		const { page = 1 } = criteria;
		const { limit = 20} = criteria;

		setIsLoading(true);
		
		if (!window.SERVER && page == 1) {
			window.scrollTo(0, 0);
		}

		const appliedFacets:ProductSearchAppliedFacet[] = [];
		if (criteria.facets) {
			for (const [key, ids] of Object.entries(criteria.facets)) {
				appliedFacets.push(
				{ 
					mapping : "attributes",
					ids
				});
			}
		}

		if (productCategory) {
			appliedFacets.push(
				{
					mapping: "productcategories",
					ids: [productCategory.id]
				}
	        );
		}
		else if (criteria.categoryId) {
			appliedFacets.push(
				{
					mapping: "productcategories",
					ids: [criteria.categoryId as int]
				}
	        );
		}

		var sorting = {
			field: "relevance",
			direction: SortDirection.DESC
		};

		if (criteria.sort && criteria.sort == "price-asc") {
			sorting = {
				field: "price",
				direction: SortDirection.ASC
			};
		} else if (criteria.sort && criteria.sort == "price-desc") {
			sorting = {
				field: "price",
				direction: SortDirection.DESC
			};
		} else if (criteria.sort && criteria.sort == "popular") {
			sorting = {
				field: "popular",
				direction: SortDirection.DESC
			};
		}

		var parameters = { ...customParameters };

		var newproducts = await searchApi.faceted({
			query: criteria.q || '',
			pageOffset: (page - 1) as int,
			searchType: ProductSearchType.Reductive,
			pageSize: limit  as int,
			minPrice: criteria.min ? criteria.min * 100 : undefined,
			maxPrice: criteria.max ? criteria.max * 100 : undefined,
			inStockOnly: criteria.inStockOnly || false,
			includePriceStats: true,
			includeDynamicBoosts: true,
			hideInactiveProducts: true,
            excludedIds: undefined,
			customParameters: parameters,
			sortOrder : sorting,
			isAdminPanel: false, // not the admin panel, and (legally) silences the type mismatch squiggly. 
			appliedFacets
		}, [
                productApi.includes.primaryurl,
				productApi.includes.calculatedprice,
				productApi.includes.primaryCategory,
				productApi.includes.businessproductconfig,

				// Plus then includes on the facets (the attribute and category selectors)
				productApi.includes.productCategoryFacets,
				productApi.includes.productCategoryFacets.category,
				productApi.includes.productCategoryFacets.category.primaryurl,
				productApi.includes.attributeValueFacets.value.attribute.attributeGroup
		]);
	
        // console.log('searching', criteria, newproducts);

		if (page == 1) {
			setProducts(newproducts);
		}
		else if (newproducts && newproducts.results && newproducts.results.length > 0) {
			setProducts(prev => {
				const safePrev = prev ?? { results: [] };

				const seen = new Set(safePrev.results.map(p => p.Id));
				const merged = [...safePrev.results];
				for (const p of newproducts.results) {
					if (!seen.has(p.id)) {
						seen.add(p.id);
						merged.push(p);
					}
				}
				newproducts.results = merged;
				return newproducts;
			});
		}
		else {
			// nothing new so 
			// leave the product set the same 
		}		

		// if overall product set has changed reset price bands
		if (criteria.resetPrices)
		{
			console.log('reset prices');
			const facets = (newproducts.secondary as SecondaryIncludes);
			const {productPriceFacets} = facets;

			// extract price facets 
			const priceFacets = (productPriceFacets?.results || []) as ProductPriceFacet[];
			const priceFacetMin = priceFacets.find(x => x.key === "min")?.value ?? null;
			if (priceFacetMin) {
				if (minPrice && priceFacetMin > minPrice) {
					setMinPrice(priceFacetMin);
				}

				setLowestPrice(priceFacetMin);
			}

			const priceFacetMax = priceFacets.find(x => x.key === "max")?.value ?? null;
			if (priceFacetMax) {
				if (maxPrice && priceFacetMax < maxPrice) {
					setMaxPrice(priceFacetMax);
				}
				setHighestPrice(priceFacetMax);
			}
		}

		// need to stash criteria for lazy loading
		setCurrentCriteria(criteria);

		setCurrentPage(page);
		setHasMore(newproducts && newproducts.results && newproducts.results.length > 0);
		setIsLoading(false);
	}

	if (!products) {
		return <Loading />;
	}

	let step = Math.round((highestPrice - lowestPrice) / 20);

	const facets = (products.secondary as SecondaryIncludes);
	const { attributeValueFacets, productCategoryFacets } = facets;
	
	//console.log('facets', facets);
	
	const categoryFacets = (productCategoryFacets?.results || []) as ProductCategoryFacet[];
	const attributeFacets = (attributeValueFacets?.results || []) as AttributeValueFacet[];
	
	var attributeMap = new Map<uint, AttributeFacetGroup>();

	attributeFacets.forEach(facet => {
		if (!(facet.value?.attribute)) {
			return;
		}

		var attribId = facet.value.productAttributeId;
		var grouping = attributeMap.get(attribId);

		if (!grouping) {
			grouping = {
				attribute: facet.value.attribute,
				facetValues: []
			} as AttributeFacetGroup;

			attributeMap.set(attribId, grouping);
		}

		grouping.facetValues.push(facet);
	});

	const attributeFacetGroups = Array.from(attributeMap.values());

	let GBPound = new Intl.NumberFormat('en-GB', {
		style: 'currency',
		currency: 'GBP',
		minimumFractionDigits: 0,
		maximumFractionDigits: 0
	});

	// moved breadcrumbs map function out of 
	// the component props and into a seperate
	// const up here, the ticket requires a home link
	// so added the requirements in below. 
	const breadcrumbs = productCategory ? [
		{
			name: 'Home',
			href: '/'
		}, 
		...(productCategory.breadcrumb ?? []).map(breadcrumb => {
			return ({
				name: breadcrumb.id === ROOT_CATEGORY_ID ? `All products` : breadcrumb.name,
				href: breadcrumb.primaryUrl
			})
		})
	]: [];

	let locale = session.locale ? session.locale.code : undefined;	
	const resultCount = (products?.totalResults || 0).toLocaleString(locale);

    // get values from url/router criteria
	const showInStockOnly = !!criteria.inStockOnly;
	const showHiddenProducts = criteria.hiddenProducts != undefined ? criteria.hiddenProducts : false;
	const sortOrder = criteria.sort || 'relevance';
	const viewStyle = criteria.view || 'small-thumbs';

	var selectedFacets:ProductAttributeValue[] = [];
	if (criteria && criteria.facets) {
		selectedFacets = Object.entries(criteria.facets).flatMap(([key, values]) =>
			values.map(v => ({
				productAttributeId: Number(key) as uint,
				id: v
			}))
		);

		console.log('selected facets' , selectedFacets);
	}
	
	const changeView = (viewType: string) => {
		store.set('productViewType', viewType);
		setCriteria({ view: viewType });
	}

	const changeSort = (sortType: string) => {
		if (sortType == "relevance") {
			//store.remove('productSortType');
			setCriteria({ sort: undefined, page: undefined });       
		} 
		else 
		{
			//store.set('productSortType', sortType);
			setCriteria({ sort: sortType, page: undefined });      
		}
	}

	//console.log("min: ", lowestPrice);
	//console.log("max: ", highestPrice);
	//console.log("defaultFrom: ", minPrice);
	//console.log("defaultTo: ", maxPrice || highestPrice);

	return (
		<>
			<div className="ui-component--padded-width">
				{productCategory && <>
					<Breadcrumb crumbs={breadcrumbs} />
				</>}

				<div className="ui-product-search">
					<h1 className="ui-page__title ui-product-search__title">
						{criteria && criteria.q ? (
							<>
								{`${resultCount} results for `}
								<strong>{criteria.q}</strong>
								{
									// when the search is contextual, show "in {categoryName}"
									productCategory ? <>
										{` in `}
										<Link className={'contextual-category'} href={productCategory.primaryUrl}>{productCategory.name}</Link>
									</> 
									// otherwise just leave it at "{resultCount} results for {query}"	
									: null
								}
							</>
						): 
							// else we show a default found x results
							`${resultCount} results`}
					</h1>

					<Button sm outlined className="ui-product-search__filter-trigger" popoverTarget="filters_popover">
						<i className="fr fr-cog"></i>
						<span>
							{`Filters`}
						</span>
					</Button>

					{/* tabletPortraitVisible? */}
					<Popover className="ui-product-search__filters-wrapper" method="auto" id="filters_popover" alignment="left"
						tabletLandscapeVisible backgroundActive={false}>
						<header className="ui-product-search__filters-header">
							<h2 className="ui-page__subtitle">
								<i className="fr fr-cog"></i>
								<span>
									{`Filters`}
								</span>
							</h2>
							<Button xs outlined className="ui-product-search__filters-close" popoverTarget="filters_popover" popoverTargetAction="hide">
								<i className="fr fr-times"></i>
								<span className="sr-only">
									{`Close`}
								</span>
							</Button>
						</header>
						<div className="ui-product-search__filters">
							<fieldset>
								<legend className="sr-only">
									{`Show / hide products`}
								</legend>
								<Input type="checkbox" onChange={(ev) => setCriteria({ inStockOnly: (ev.target as HTMLInputElement).checked ? true : undefined, page: undefined })} isSwitch flipped label={`Only show in stock`} checked={showInStockOnly} name="show-in-stock" noWrapper />
							</fieldset>

							{(productCategory) ?
								<fieldset>
									<legend className="sr-only">
										{`All product categories`}
									</legend>
									<CategoryFilters collection={products} currentCategory={productCategory} />
								</fieldset>
								:
								<>
									{categoryFacets.length > 0 &&
										<fieldset>
											<legend>
												{`Categories`}
											</legend>
											<div className="fieldset-content">
												<Input type="search" placeholder={`Search for ...`} value={categorySearch} onChange={(e) => setCategorySearch(e.target.value)} noWrapper />
												<CategoryFilterList facets={categoryFacets} maxVisible={MAX_VISIBLE_CATEGORIES} searchFilter={categorySearch} noBorder />
											</div>
										</fieldset>
									}
								</>
							}

							{allowReset && hasCriteria() &&
								<fieldset>
									<Button xs outlined className="ui-product-search__filters-reset" onClick={() => {
										clearAllCriteria(['approvalStatus','inStockOnly', 'hiddenProducts']);

										// code smell: hack to ensure price range updates
										document.location = document.location.href;
									}}>
										{`Reset all filters`}
									</Button>
								</fieldset>
							}

							{highestPrice && 
								<DualRange
									className="ui-product-search__price"
									label={`Price`}
									numberFormat={GBPound}
									min={lowestPrice}
									max={highestPrice}
									step={step}
									defaultFrom={minPrice}
									defaultTo={maxPrice || highestPrice}
									onChange={(from: number, to: number) => {
										var range: PriceRange = {};
										if (from && from != lowestPrice) {
											range.min = from;
										} else {
											range.min = undefined;
										}
										if (to && to != highestPrice) {
											range.max = to;
										} else {
											range.max = undefined;
										}
										setCriteria(range);
									}}
								/>
							}

							{showDebug &&
								<div>
									{`filter ${minPrice} to ${maxPrice}`}
									<br></br>
									{`range  ${lowestPrice} to ${highestPrice} -  ${step}`}
								</div>
							}

							{/* attributes */}
							{attributeFacetGroups.length > 0 && <>
								{
									attributeFacetGroups.map(facet => {
										// can include primaryUrl etc on facets as well if needed
										// here though it is (probably, I haven't looked at the designs recently) exclusively a button
										// which then restricts the search.

										return <>
											<fieldset key={facet.attribute.id}>
												<legend>
													{facet.attribute.name}
												</legend>
												<FilterList
													selectedAttributeValues={selectedFacets}
													facets={facet.facetValues}
													units={facet.attribute.units}
													maxVisible={4 as int}
													setSelectedAttributeValues={(selectedFacets) => {
														handleUpdatedFacets(selectedFacets);
													}}
												/>
											</fieldset>
										</>;
									})
								}
							</>}
						</div>
					</Popover>

					<header className="ui-product-search__header">

						{/* TODO: sort options */}
						<Input type="select" aria-label={`Sort by`} value={sortOrder} noWrapper className="form-select ui-form-select ui-form-select--sm" onChange={(ev) => {
							var sortValue = (ev.target as HTMLInputElement).value;
							changeSort(sortValue);
						}}>
							<option value="relevance">
								{`Relevance`}
							</option>
							<option value="popular">
								{`Most Purchased`}
							</option>    
							<option value="price-desc">
								{`Price (highest to lowest)`}
							</option>
							<option value="price-asc">
								{`Price (lowest to highest)`}
							</option>
						</Input>

						{/* view style switch */}
						<div className="btn-group btn-group-sm ui-btn-group" role="group" aria-label={`Select view style`}>
							<Input
								type="radio"
								noWrapper label={`List`}
								groupIcon="fr-list"
								groupVariant="primary"
								value='list'
								checked={viewStyle == 'list'}
								onChange={() => {
									changeView('list')
								}}
								name="view-style"
							/>
							<Input
								type="radio"
								noWrapper
								label={`Small thumbnails`}
								groupIcon="fr-th-list"
								groupVariant="primary"
								value='small-thumbs'
								checked={viewStyle == 'small-thumbs'}
								onChange={() => {
									changeView('small-thumbs')
								}}
								name="view-style"
							/>
							<Input type="radio"
								noWrapper
								label={`Large thumbnails`}
								groupIcon="fr-grid"
								groupVariant="primary"
								value='large-thumbs'
								checked={viewStyle == 'large-thumbs'}
								onChange={() => {
									changeView('large-thumbs')
								}}
								name="view-style"
							/>
						</div>
					</header>

					<ProductList
						content={products.results}
						viewStyle={viewStyle}
						
						// ========================================================
						// Additional Components: Inject
						// --------------------------------------------------------
						// inject the additional components, at this point 
						// they're a pure component array ready to be called 
						// as
						// --- JS -------------------------------------------------
						// {productExtras.map(
						//        (Component) => <Component product={product}/>
						// )}
						productExtras={additionalComponents}
						// ========================================================
						paginator={(
							<div className={'pagination-container'}>
								{lazyLoad ?
									<LazyLoader
										isLoading={isLoading}
										onLoadMore={handleLoadMore}
										hasMore={hasMore}
									/>
									:
									<LoadMore
										isLoading={isLoading}
										onLoadMore={handleLoadMore}
										hasMore={hasMore}
									/>
								}
							</div>
						)}

					/>
				</div>
			</div>

		</>
	);
}

export default Search;