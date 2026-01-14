import {Product} from "Api/Product";
import {ProductCategory} from "Api/ProductCategory";
import {ProductCategoryFacet} from "UI/Product/Search/Facets";
import Link from 'UI/Link'
import Button from 'UI/Button'

import { useMemo, useState } from "react";
import {ApiList} from "UI/Functions/WebRequest";
import {useRouter} from "UI/Router";

const ROOT_CATEGORY_ID: uint = 1 as uint;

// basic props type for the category filters component.
export type CategoryFilterProps = {
	// this is the direct response from the API
	// this also must have the category facets.
	// this gets checked in the component.
	collection: ApiList<Product>,

	// the current 
	currentCategory: ProductCategory;
}

// represents a tree branch structure
// for category tree node.
export type CategoryTreeNode = {
	// the facet (used for count etc...)
	facet: ProductCategoryFacet;

	// any child categories. 
	children: ProductCategory[];
}

// This removes any query string items
// that are generally stale between category navigation.
// Currently removes the page number, but can be extended.
const filterResettableQueryStringItems = (params: URLSearchParams) => {
	if (!params) {
		return params;
	}

	const removableItemKeys = ["page"];
	const newParams = new URLSearchParams();

	params.forEach((value: string, key: string) => {
		if (!removableItemKeys.includes(key)) {
			newParams.append(key, value);
		}
	});

	return newParams;
};


const CategoryFilters: React.FC<CategoryFilterProps> = (props: CategoryFilterProps) => {

	// in order to display the category filters, we need the product collection,
	// we also need the current category we can collect the direct children
	// of the current category and collect the parent categories 
	// and build the category tree to work the same as amazon.
	const { collection, currentCategory } = props;

	// parent state may change, but the collection isn't guaranteed to have changed, 
	// store this in a useMemo to avoid state usage and a useEffect to populate.
	const categoryTree = useMemo<Map<uint, CategoryTreeNode>>(
		() => buildCategoryTree(collection),
		[collection]
	);

	// this is all categories **ABOVE** the current one
	// this **DOES NOT** include the current category.
	const parentCategoryPath = useMemo<ProductCategory[]>(
		() => getParentCategoryPath(currentCategory, categoryTree),
		[currentCategory, categoryTree]
	);

	// we've pulled this in to make sure the
	// search query is persistent in the URL
	// so when somebody clicks on a category
	// the search query is retained.
	const { pageState } = useRouter();

	// the top level categories can often have a lot of children
	// so create a state item to hold the limit.
	const [maxCategoryListing, setMaxCategoryListing] = useState(5);

	// get the root tree item, the root category has a special condition
	const rootTreeItem = categoryTree.get(ROOT_CATEGORY_ID);

	// the actual root category
	const root = rootTreeItem?.facet.category;

	// try and get child categories from the current one.
	// this will ALWAYS be an array, but to satisfy TypeScripts
	// safety concerns, I've added a ?? [] to guarantee there is 
	// always an array value.
	const children: ProductCategory[] = categoryTree.get(currentCategory.id)?.children ?? [];

	// now we're passed hooks we can avoid the conditional hooks issue
	// both useMemos are designed with collection & currentCategory being
	// null, we'll exist early here when the current category isn't available
	// or the ApiList<Product> collection, as these 2 params are 
	// required to make the component work properly.
	if (!currentCategory || !collection || !root) {
		return null;
	}

	// we don't pull this into state, it's derived from a hook
	// it's lifecycle is managed elsewhere, and query edits
	// are debounced and cause a state update which in effect
	// re-renders this. This is derived from pageState
	// due to window.location not being SSR safe. 
	let queryString = filterResettableQueryStringItems(pageState.query).toString();

	// just in-case the toString() doesn't prepend 
	// the query string start delimiter, we add one in.
	// this simplifies code further down too.
	if (!queryString.startsWith('?')) {
		queryString = '?' + queryString;
	}

	const renderRootCategory = () => {
		return <>
			{children.map((category, idx) => {

				if (idx >= maxCategoryListing) {
					// hide any excess.
					return;
				}

				const treeNode = categoryTree.get(category.id);

				return (
					<li className="category-treeview__item">
						<Link xs href={category.primaryUrl + queryString}>
							<i className='fr fr-fw fr-tag' />
							<span>
								{category.name} ({treeNode?.facet?.count ?? 0})
							</span>
						</Link>
					</li>
				)
			})}
			{children.length > maxCategoryListing && (
				<li className="category-treeview__item category-treeview__item--more">
					<Button xs variant="link" onClick={() => setMaxCategoryListing(prev => Math.min(prev + 5, children.length))}>
						<i className='fr fr-fw' />
						<span>
							{`Show ${Math.min(5, children.length - maxCategoryListing)} more`}
						</span>
					</Button>
				</li>
			)}
		</>;
	}

	const renderNormalCategory = () => {

		return <>
			<li className="category-treeview__item">
				<Link xs href={root.primaryUrl + queryString}>
					<i className="fr fr-fw fr-arrow-180"></i>
					<span>
						{`All products`}
					</span>
				</Link>
			</li>
			{
				// alike amazon, parent categories are stacked 
				// so we'll mimic the same behaviour here. 
				// the root category isn't in this array
				parentCategoryPath.map(category => {
					return (
						<li className="category-treeview__item">
							<Link xs href={category.primaryUrl + queryString}>
								<i className="fr fr-fw fr-arrow-90"></i>
								<span>
									{`${category.name} (${categoryTree.get(category.id)?.facet.count ?? 0})`}
								</span>
							</Link>
						</li>
					)
				})
			}

			<li className="category-treeview__item">
				<ul className="category-treeview__item">
					<li className="category-treeview__item">
						<Link xs href={'#'}>
							{children.length != 0 && <i className='fr fr-fw fr-chevron-down' />}
							{children.length == 0 && <i className='fr fr-fw fr-tag' />}
							<span>
								{`${currentCategory.name} (${categoryTree.get(currentCategory.id)?.facet.count ?? 0})`}
							</span>
						</Link>
					</li>
					<ul className="category-treeview__item">
						{children.map((category, idx) => {

							if (idx >= maxCategoryListing) {
								// hide any excess.
								return;
							}

							return (
								<li className="category-treeview__item">
									<Link xs href={category.primaryUrl + queryString}>
										<i className='fr fr-fw fr-tag' />
										<span>
											{`${category.name} (${categoryTree.get(category.id)?.facet.count ?? 0})`}
										</span>
									</Link>
								</li>
							)
						})}
						{children.length > maxCategoryListing && (
							<li className="category-treeview__item category-treeview__item--more">
								<Button xs variant="link" onClick={() => setMaxCategoryListing(prev => Math.min(prev + 5, children.length))}>
									<i className='fr fr-fw' />
									<span>
										{`Show ${Math.min(5, children.length - maxCategoryListing)} more`}
									</span>
								</Button>
							</li>
						)}

					</ul>
				</ul>
			</li>
		</>;
	}

	return (
		<ul className="category-treeview">
			{currentCategory?.id == root?.id ? renderRootCategory() : renderNormalCategory()}
		</ul>
	)
}

const getParentCategoryPath = (category: ProductCategory, categoryTree: Map<uint, CategoryTreeNode>): ProductCategory[] => {

	// initialise an empty array ready for population
	const pathItems: ProductCategory[] = [];

	// if the immediate category has no parent
	// then return an empty array
	if (!category?.parentId || category.parentId === ROOT_CATEGORY_ID) {
		return pathItems;
	}

	// initialise current ready for a while statement. 
	// this gets the initial parent category. 
	let current = categoryTree.get(category.parentId);

	// the initial parent may be null, 
	// in this instance, same as above; return
	// an empty array.
	if (!current) {
		return pathItems;
	}

	// now we execute a while, 
	// moving around the tree
	// collection categories 
	// until no parent exists.
	while(current)
	{
		// push it to the path items array
		pathItems.push(current.facet.category);

		// if the current category has no parent
		// we've already pushed it to the pathItems
		// array, so we can break the loop here
		// and go straight to returning the path items.
		// we also ignore the root category here.
		if (!current.facet.category.parentId || current.facet.category.parentId === ROOT_CATEGORY_ID) {
			break;
		}
		// otherwise ascend to the parent category.
		current = categoryTree.get(current.facet.category.parentId)
	}

	// return the pathItems, they also need to be reversed 
	// as they start from the bottom now they here
	// * drake wants to know your location *
	// at the moment they'd probably look like this
	// [1133, 1132, 1110, 29, 5] so lets reverse them
	// [5, 29, 1110, 1132, 1133] looks better. 
	return pathItems.reverse();

}

/**
 * Takes an ApiList<Product> with secondary includes & category facets
 * and builds a tree out of them.
 * @param {ApiList<Product>>} collection
 * @return {Map<uint, CategoryTreeNode>} - uint being the category ID, category tree node being the actual category/facet/children
 */
const buildCategoryTree = (collection: ApiList<Product>): Map<uint, CategoryTreeNode> => {

	// create a map so all categories only have one instance.
	const map = new Map<uint, CategoryTreeNode>();

	if (!collection) {
		// the collection may not have loaded yet. 
		// when the component first mounts, the useEffect
		// that populates it doesn't run until the component has mounted
		// and requires a response from an endpoint, which is also an async
		// function, meaning the product collection will not be available until
		// at very least the 2nd execution of the useEffect calling it.
		return map;
	}

	// skip the '?' on the collection variable, we know its value-ful otherwise
	// the "if" branch above would stop the executing by returning early.
	if (!collection.secondary?.productCategoryFacets) {
		// the current request doesn't have this facet enabled, 
		// leave a message in the log and return out. 

		console.error('[CategoryFilters] Collection has no secondary includes, a category tree cannot be built without the secondary includes', { name: 'productCategoryFacets' });
		return map;
	}
	// also grab the facets
	const facets           = collection.secondary.productCategoryFacets.results;

	// first iterate the categories, fill in the map,
	// this doesn't populate children, but adds the category and its facet information
	// it also initialises an empty array of children, so calling .children will always
	// result in an array.
	facets.forEach((facet: ProductCategoryFacet) => {
		if (!facet?.category) {
			return;
		}

		map.set(facet.category.id, {
			facet,
			children: []
		});
	})
	// second time around we have all the relevant category data setup, 
	// we iterate over entries, 
	// ignore any TS errors saying "forEach" doesn't exist on 
	// MapIterator<uint, CategoryTreeNode>, it most certainly does.
	// the entry is destructured here, noticed the first argument
	// is omitted, this is by design, the node represents the 
	// node holding the facet & children
	map.entries().forEach(([, node]) => {

		// destructure facet from the node, each node has facet & children
		const { facet } = node;

		// get the parentId, fallback to the root category ID.
		const parentId = (facet.category.parentId) || ROOT_CATEGORY_ID;

		// get the parent node, this differs from the last approach 
		// as instead of performing a ".filter" and increasing the amount
		// of operations being run, every category gets iterated over
		// this appraoch reduces the amount of work.
		const parentNode = map.get(parentId);

		// this should error out
		// the parent ID can either be a non-zero value
		// which is true, which means the next branch will
		// skip, or it will fall back to the root category ID
		// if the ID is falsy (empty, null or undefined) 
		if (!parentNode) {
			throw new Error("Couldn't identify the target category and failed to fallback on the root category ID")
		}

		// push the category to the parentNode children collection
		parentNode!.children.push(facet.category)
	})

	return map;
}

export default CategoryFilters;