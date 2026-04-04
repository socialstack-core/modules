import { useEffect, useState, useRef } from "react";
import { useRouter } from "UI/Router";
import { useSession } from 'UI/Session';

import TreeView, { buildBreadcrumbs } from "Admin/TreeView";
import Input from "UI/Input";
import Image from "UI/Image";
import Loading from "UI/Loading";
import { MultiSelectBox } from "./MultiSelect";

import productCategoryApi, {ProductCategory} from "Api/ProductCategory";
import searchApi, {ProductSearchAppliedFacet, ProductSearchType, SortDirection} from "Api/ProductSearchController";
import { ProductIncludes} from "Api/Includes";
import productApi, { Product } from "Api/Product";
import { ApiList } from "UI/Functions/WebRequest";
import { ProductAttribute } from "Api/ProductAttribute";
import { ProductAttributeValue } from "Api/ProductAttributeValue";
import Link from "UI/Link";
import {SortField} from "Admin/AutoList";
import Time from "UI/Time";
import Debounce from "UI/Functions/Debounce";
import Paginator from "UI/Paginator";
import { ProductCategoryFacet } from "UI/Product/Search/Facets";
import AdminPage from 'Admin/AdminPage';
import Footer from 'Admin/Footer';

/**
 * Props for the ProductCategoryTree component
 * @typedef {Object} ProductCategoryTreeProps
 * @property {boolean} noCreate - Determines whether the "New category" and "New product" buttons are shown
 */
export type ProductCategoryTreeProps = {
	noCreate: boolean;
};

/**
 * View type for the main page
 * @typedef {"list" | "tree"} PageViewType
 */
type PageViewType = "list" | "tree";

/**
 * Displays the product category tree or product list view with search and filter functionality.
 *
 * @component
 * @param {ProductCategoryTreeProps} props - Component props
 * @returns {JSX.Element}
 */
export default function ProductCategoryTree({ noCreate }: ProductCategoryTreeProps) {

	const { pageState, updateQuery } = useRouter();

	const queryText = pageState.query?.get("q");
	const [viewType, setViewType] = useState<PageViewType>(queryText ? "list" : "tree");
	const path = pageState.query?.get("path") || "";
	const breadcrumbs = buildBreadcrumbs("/en-admin/product", "Products", path, "/en-admin/product");

	const updateQueryRef = useRef(updateQuery);
	const debounce = useRef(
		new Debounce(
			(query: string) => {
				updateQueryRef.current({ q: query });
			}
		)
	);

	updateQueryRef.current = updateQuery;

	const addProductUrl = "/en-admin/product/add";
	const addProductTemplateUrl = "/en-admin/producttemplate/add";
	const addCategoryUrl = "/en-admin/productcategory/add";

	return (
		<>
			<AdminPage.SubHeader
				title={`Edit Products`}
				className="admin-product-category-tree__subheader"
				breadcrumbs={breadcrumbs}>

				<div className="product-search">
					<Input
						type="search" noWrapper
						defaultValue={queryText}
						onChange={(ev) => {
							debounce.current.handle((ev.target as HTMLInputElement).value)
							setViewType("list")
						}}
						onFocus={() => {
							setViewType("list");
						}}
						placeholder={`Filter products`}
					/>
				</div>

				<div className="btn-group btn-group-sm ui-btn-group view-toggle" role="group" aria-label={`Select view style`}>
					<Input
						type="radio"
						noWrapper
						label={`Tree`}
						groupIcon="fr-grid"
						groupVariant="primary"
						value='tree'
						checked={viewType == 'tree'}
						onChange={() => setViewType('tree')}
						name="view-style"
					/>
					<Input
						type="radio"
						noWrapper
						label={`List`}
						groupIcon="fr-th-list"
						groupVariant="primary"
						value='list'
						checked={viewType == 'list'}
						onChange={() => setViewType('list')}
						name="view-style"
					/>
				</div>

			</AdminPage.SubHeader>

			<AdminPage.ContentWrapper>
				<AdminPage.Content className="admin-payments-product-list">
					{viewType === "tree" ?
						<TreeView onLoadData={(path) => productCategoryApi.getTreeNodePath(path).then((resp) => resp)} /> :
						<ProductListView />
					}
				</AdminPage.Content>
			</AdminPage.ContentWrapper>

			{!noCreate && (
				<Footer>
					<Link href={addCategoryUrl} variant="primary" outlined>
						{`New category`}
					</Link>
					<Link href={addProductTemplateUrl} variant="primary" outlined>
						{`New product template`}
					</Link>
					<Link href={addProductUrl} variant="primary">
						{`New product`}
					</Link>
				</Footer>
			)}

		</>
	);
}

/**
 * Props for ProductListView
 * @typedef {Object} ProductListViewProps
 * @property {string} [query] - Optional search query
 */
type ProductListViewProps = {
};

/**
 * Displays a list view of products, including filtering and search functionality.
 *
 * @component
 * @param {ProductListViewProps} props - Component props
 * @returns {JSX.Element}
 */
const ProductListView: React.FC<ProductListViewProps> = (props: ProductListViewProps) => {
	const [searchResults, setSearchResults] = useState<ApiList<Product>>();
	const [selectedAttributeValues, setSelectedAttributeValues] = useState<ProductAttributeValue[]>([]);
	const [selectedCategories, setSelectedCategories] = useState<ProductCategory[]>([]);
	const [loading, setLoading] = useState<boolean>(false);
	const [sortOrder, setSortOrder] = useState<SortField>({ field: null, direction: 'asc' }); // relevance by default
	const { pageState, updateQuery } = useRouter();
    const { session } = useSession();
	const { query } = pageState;
	const queryText = query?.get("q");

	// grab the current page
	const currentPage: uint = (pageState.query?.has("page") ? parseInt(pageState.query?.get("page") ?? "1") : 1) as uint;

	// and the page size.
	const pageSize: uint = (pageState.query?.has('limit') ? parseInt(pageState.query?.get("limit") ?? "10") : 20) as uint;

	useEffect(() => {
		setLoading(true);

		const appliedFacets = [] as ProductSearchAppliedFacet[];

		if (selectedAttributeValues.length) {
			// Group them up by attribute. This is important because ultimately the server wants to make a query of the form..
			// (Red or Blue or Green) and (200mm or 100mm)
			// The server provides this or/and behaviour via or'ing inside each appliedFacet
			// whilst and'ing the appliedFacets together.

			// Map of attribId -> attribValueIds.
			const uniqueAttributeMap: Map<int, int[]> = new Map<int, int[]>();

			selectedAttributeValues.forEach(attribValue => {
				const attributeId = attribValue.productAttributeId;
				let attributeValueSet = uniqueAttributeMap.get(attributeId);

				if (!attributeValueSet) {
					attributeValueSet = [];
					uniqueAttributeMap.set(attributeId, attributeValueSet);
				}

				attributeValueSet.push(attribValue.id);
			});

			// Next for each attribute value group, add that appliedFacet.
			uniqueAttributeMap.forEach((attribValueIds) => {
				appliedFacets.push({
					mapping: 'attributes',
					ids: attribValueIds
				});
			});
		}

		if (selectedCategories.length) {
			appliedFacets.push({
				mapping: 'productcategories',
				ids: selectedCategories.map(cat => cat.id)
			});
		}
		
		if (!queryText && !selectedCategories.length && !appliedFacets.length) {
			appliedFacets.push({
				mapping: 'productcategories',
				ids: [1]
			})
		}

		searchApi
			.faceted({
				query: queryText ?? '',
				pageOffset: (currentPage - 1) as uint,
				pageSize: pageSize,
				appliedFacets,
				minPrice: undefined,
				maxPrice: undefined,
				inStockOnly: false,
                isAdminPanel:true,
				searchType: ProductSearchType.Reductive,
				sortOrder: {
					field: sortOrder.field,
					direction: sortOrder.direction === 'asc' ? SortDirection.ASC : SortDirection.DESC
				}
			}, [
				productApi.includes.attributeValueFacets,
				productApi.includes.productCategoryFacets,
				productApi.includes.attributes.attribute,
				productApi.includes.productcategories,
				productApi.includes.productCategoryFacets.category,
			])
			.then((response) => {
				setSearchResults(response);
				setLoading(false);
			});
	}, [selectedAttributeValues, selectedCategories, queryText, sortOrder, pageSize, currentPage]);

	const changeSortField = (field: string) => {
		if (field == sortOrder.field) {
			sortOrder.direction = sortOrder.direction === 'asc' ? 'desc' : 'asc';
			setSortOrder({...sortOrder});
			return;
		}
		setSortOrder({
			field, direction: 'asc'
		})
	}

	let locale = session.locale ? session.locale.code : undefined;	
	const resultCount = (searchResults?.totalResults || 0).toLocaleString(locale);

	if (loading) {
		return <Loading />;
	}

	return <>
		<div className="admin-payments-product-list__collection">
			{searchResults?.secondary && (
				<SearchAttributeFilter
					results={searchResults}
					selectedAttributeValues={selectedAttributeValues}
					setSelectedAttributeValues={setSelectedAttributeValues}
					selectedCategories={selectedCategories}
					setSelectedCategories={setSelectedCategories}
				/>
			)}

			{searchResults?.totalResults > 0 &&
				<>
					<div>
						{`${resultCount} results`}
					</div>

					<Paginator
						totalResults={searchResults?.totalResults}
						pageSize={pageSize}
						pageIndex={currentPage}
						onChange={(toPage: number) => {
							updateQuery({ page: toPage.toString() })
						}}
					/>
				</>
			}
			<table className="table ui-table">
				<thead>
					<tr>
						<th
							onClick={() => changeSortField('Id')}
							className={sortOrder.field === 'Id' ? 'active' : ''}
						>
							Id
							{sortOrder.field === 'Id' ? <i className={'fas fa-chevron-' + (sortOrder.direction == 'asc' ? 'up' : 'down')} /> : null}
						</th>
						<th>Image</th>
						<th
							onClick={() => changeSortField('Name.en')}
							className={sortOrder.field === 'Name.en' ? 'active' : ''}
						>
							Name
							{sortOrder.field === 'Name.en' ? <i className={'fas fa-chevron-' + (sortOrder.direction == 'asc' ? 'up' : 'down')} /> : null}
						</th>
						<th
							onClick={() => changeSortField('Sku')}
							className={sortOrder.field === 'Sku' ? 'active' : ''}
						>
							SKU
							{sortOrder.field === 'Sku' ? <i className={'fas fa-chevron-' + (sortOrder.direction == 'asc' ? 'up' : 'down')} /> : null}
						</th>
						<th
							onClick={() => changeSortField('CreatedUtc')}
							className={sortOrder.field === 'CreatedUtc' ? 'active' : ''}
						>
							Created
							{sortOrder.field === 'CreatedUtc' ? <i className={'fas fa-chevron-' + (sortOrder.direction == 'asc' ? 'up' : 'down')} /> : null}
						</th>
						<th
							onClick={() => changeSortField('EditedUtc')}
							className={sortOrder.field === 'EditedUtc' ? 'active' : ''}
						>
							Edited
							{sortOrder.field === 'EditedUtc' ? <i className={'fas fa-chevron-' + (sortOrder.direction == 'asc' ? 'up' : 'down')} /> : null}
						</th>

						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
					{searchResults?.results.map((product) => (
						<tr key={product.id}>
							<td>{product.id}</td>
							<td>{product.featureRef ? <Image fileRef={product.featureRef} /> : `No image available`}</td>
							<td>{product.name}</td>
							<td>{product.sku}</td>
							<td><Time date={product.createdUtc} /></td>
							<td><Time date={product.editedUtc} /></td>
							<td>
								<Link xs outlined variant="primary" href={'/en-admin/product/' + product.id}>
									{`Edit product`}
								</Link>
							</td>
						</tr>
					))}
				</tbody>
			</table>
			<Paginator
				totalResults={searchResults?.totalResults}
				pageSize={pageSize}
				pageIndex={currentPage}
				onChange={(toPage: number) => {
					updateQuery({ page: toPage.toString() })
				}}
			/>
		</div>
	</>;
};

/**
 * Props for SearchAttributeFilter component
 * @typedef {Object} ProductAttributeFilterProps
 * @property {ApiList<Product>} results - Search results with includes
 * @property {ProductSearchAppliedFacet[]} value - Current filter values
 */
type ProductAttributeFilterProps = {
	results: ApiList<Product>;

	/**
	 * An array of selected attribute values (red, green, whatever)
	 */
	selectedAttributeValues: ProductAttributeValue[],

	/**
	 * Do something when the attribs change.
	 */
	setSelectedAttributeValues: (values: ProductAttributeValue[]) => void

	/**
	 * An array of selected categories
	 */
	selectedCategories: ProductCategory[],

	/**
	 * Do something when the cats change.
	 */
	setSelectedCategories: (values: ProductCategory[]) => void
};

/**
 * Renders multi-select filters for product attributes.
 *
 * @component
 * @param {ProductAttributeFilterProps} props - Component props
 * @returns {JSX.Element}
 */
const SearchAttributeFilter: React.FC<ProductAttributeFilterProps> = (props: ProductAttributeFilterProps) => {
	const { results, selectedAttributeValues, setSelectedAttributeValues, selectedCategories, setSelectedCategories } = props;

	const attributeValueIds = results.secondary?.attributeValueFacets?.results;
	if (!attributeValueIds) {
		throw new Error("Missing attributeValueFacets in includes.");
	}

	const attributeValues: ProductAttributeValue[] | undefined = results.includes.find((include: { field: string }) => include.field === "attributes")?.values;
	const attributes: ProductAttribute[] | undefined = results.includes.find((include: { field: string }) => include.field === "attribute")?.values;

	if (!attributeValues || !attributes) {
		throw new Error("Missing attribute or attributeValues in includes.");
	}

	return (
		<div className="admin-payments-product-list__attribute-filters">
			<CategoryFilter 
				results={results} 
				value={selectedCategories} 
				setSelectedCategories={setSelectedCategories}
			/>
			{uniqueAttributes(attributes).map((productAttribute: ProductAttribute) => {
				const values = attributeValues.filter((val) => val.productAttributeId == productAttribute.id);
				
				return (
					<div key={productAttribute.id} className="admin-payments-product-list__attribute-filter">
						<MultiSelectBox
							onSetValue={(valueId: int, added: boolean) => {
								if (added) {
									// Add value with ID #valueId
									var valueToAdd = values.find(val => val.id == valueId);

									if (valueToAdd) {
										setSelectedAttributeValues([...selectedAttributeValues, valueToAdd]);
									}
								} else {
									// Remove it
									setSelectedAttributeValues(selectedAttributeValues.filter(val => val.id != valueId));
								}
							}}
							defaultText={productAttribute.name}
							value={selectedAttributeValues.map(sav => sav.id)}
							options={uniqueAttributeValues(values).map((val: ProductAttributeValue) => {
								return {
									value: val.value + (productAttribute.units ?? ""),
									valueId: val.id,
									count: attributeValueIds.find((attr: any) => attr.attributeValueId == val.id)?.count ?? 0
								};
							})}
						/>
					</div>
				);
			})}
		</div>
	);
};

type CategoryFilterProps = {
	results: ApiList<Product>;
	setSelectedCategories: (values: ProductCategory[]) => void;
	value: ProductCategory[]
}

const CategoryFilter: React.FC<CategoryFilterProps> = (props) => {
	
	const { results, value, setSelectedCategories } = props;
	
	const categoryFacets = results?.secondary?.productCategoryFacets.results ?? [];

	if (categoryFacets.length == 0) {
		return null;
	}

	return (
		<div className="admin-payments-product-list__attribute-filter">
			<MultiSelectBox 
				onSetValue={(valueId: int, added: boolean) => {
					if (added) {
						// Add value with ID #valueId
						var category = categoryFacets.find(val => val.category?.id == valueId)?.category;

						if (category) {
							setSelectedCategories([...value, category]);
						}
					} else {
						// Remove it
						setSelectedCategories(value.filter(val => val.id != valueId));
					}
				}} 
				defaultText={`Categories`} 
				value={value.map(cat => cat.id)} 
				options={categoryFacets.map((categoryFacet: ProductCategoryFacet) => {
					const { category } = categoryFacet;
					
					if(!category){
						return {
							value: 'Deleted category',
							count: categoryFacet.count,
							valueId: categoryFacet.productCategoryId
						};
					}
					
					return {
						value: category.name,
						count: categoryFacet.count,
						valueId: category.id,
					}
				})}
			/>
		</div>
	)
}

/**
 * Filters and returns a list of unique attributes by name.
 *
 * @param {ProductAttribute[]} attrs - List of attributes
 * @returns {ProductAttribute[]} Unique attributes
 */
const uniqueAttributes = (attrs: ProductAttribute[]): ProductAttribute[] => {
	const seen = new Set();
	return attrs.filter((attr) => {
		if (seen.has(attr.name)) return false;
		seen.add(attr.name);
		return true;
	});
};

/**
 * Filters and returns a list of unique categories.
 *
 * @param {ProductCategory[]} categories - List of attributes
 * @returns {ProductCategory[]} Unique attributes
 */
const uniqueCategories = (categories: ProductCategory[]): ProductCategory[] => {
	const unique: ProductCategory[] = [];
	
	categories.forEach((category) => {
		if (unique.find(unq => unq.id === category.id)) {
			return;
		}
		unique.push(category);
	})
	return unique.sort((a, b) => a.name!.localeCompare(b.name!, undefined, { numeric: true }));;
};

/**
 * Filters and returns unique attribute values, sorted by value.
 *
 * @param {ProductAttributeValue[]} values - List of attribute values
 * @returns {ProductAttributeValue[]} Unique, sorted attribute values
 */
const uniqueAttributeValues = (values: ProductAttributeValue[]): ProductAttributeValue[] => {
	const seen = new Set<string>();
	return values
		.filter((v) => !seen.has(v.value!) && seen.add(v.value!))
		.sort((a, b) => a.value!.localeCompare(b.value!, undefined, { numeric: true }));
};

type ParsedQuery = {
	query?: string;
	facet: Record<string, ulong[]>;
};
