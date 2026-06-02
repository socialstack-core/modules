import useApi from 'UI/Functions/UseApi';
import {ProductSearchApi as searchApi, ProductSearchType, SortDirection} from "Api/Payments";
import productApi, {Product} from 'Api/Product';
import {useState} from "react";
import SearchInput from 'UI/SearchInput';
import Button from 'UI/Button';
import Alert from 'UI/Alert';
import Image from 'UI/Image';

/**
 * Props for the Product Lookup component.
 */
interface ProductLookupProps {
	customParameters?: Record<string,any>,
	pageSize?:number,
	showInStockOnly? :boolean,
	isAdminpanel?: boolean,
	hideInactiveProducts?: boolean,
	searchPlaceholder?: string,
	actionTitle?: string,
	onSelected: (val: Product) => void;
}

/**
 * The website Product Lookup React component.
 * @param props React props.
 */
const ProductLookup: React.FC<ProductLookupProps> = (props:ProductLookupProps) => {

	const { 
		customParameters = {},
		pageSize = 10,
		showInStockOnly = false,
		hideInactiveProducts = false,
		isAdminpanel = true,
		searchPlaceholder = `Search by name, category or code`,
		actionTitle = `Add product`,
		onSelected
	} = props;

    const [query, setQuery] = useState<string>();

	const [products] = useApi(async () => {

		if (!query || !query.length) {
			return null;
		}

		const products = await searchApi.faceted({
			query: query,
			pageOffset: 0 as int,
			searchType: ProductSearchType.Reductive,
			pageSize: (pageSize ?? 10) as uint,
			inStockOnly: showInStockOnly,
			includeDynamicBoosts: false,
			hideInactiveProducts: hideInactiveProducts,
			includePriceStats: false,
			isAdminPanel: isAdminpanel,
			customParameters: customParameters,
			sortOrder: {
				field: "relevance",
				direction: SortDirection.DESC
			},
		}, [
			productApi.includes.primaryurl,
			productApi.includes.calculatedprice
		]);
		
		return products;

	},[query])


	return (
		<div className="ui-product-lookup">

			<div className="ui-product-lookup__search">
			<SearchInput
				searchPlaceholder={searchPlaceholder}
				updateQueryString={false}
				linktoSearchPage={false}
				onQueryChange={setQuery}
			/>
			</div>

			{query && query?.length != 0 && 
				<> 
					{products && products.results && products.results.length != 0 ?
					<> 
						<table className="ui-product-lookup__results">
							{products.results.map(content => {
								return 	<tr key={String(content.id)} className="ui-product-lookup__item">
									<td>
										{content.featureRef && <Image size={32} fileRef={content.featureRef} className="ui-product-lookup__img" />}
									</td>
									<td>{content.sku}</td>
									<td>
										{content.name}
									</td>
									<td>
										{onSelected &&
											<span className="ui-product-lookup__item-action">
												<Button outlined onClick={() => onSelected(content)}>
													{actionTitle}
												</Button>
											</span>
										}
											</td>
									</tr>
							})}
						</table>
					</> : 
						<Alert variant="info">
							{`No matching products found`}
						</Alert>
					}
				</>
			}

		</div>
	);
}

export default ProductLookup;
