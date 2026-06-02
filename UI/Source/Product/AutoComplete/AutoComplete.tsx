import useApi from 'UI/Functions/UseApi';
import { ProductSearchApi as searchApi, ProductSearchType, SortDirection } from "Api/Payments";
import productApi, {Product} from 'Api/Product';
import Link from 'UI/Link';
import Image from 'UI/Image';
import Quantity from 'UI/Product/Quantity';
import Alert from 'UI/Alert';
import {addToRecentSearches} from "UI/RecentSearches/SearchHistory";

/**
 * Props for the AutoComplete component.
 */
interface AutoCompleteProps {
	customParameters?: Record<string, any>,
	query: string,
	pageSize?: number,
	showInStockOnly?: boolean
}

/**
 * The AutoComplete React component.
 * @param props React props.
 */
const AutoComplete: React.FC<AutoCompleteProps> = (props:AutoCompleteProps) => {

	const { 
		customParameters = {},
        query,
        pageSize = 10,
		showInStockOnly = false
	} = props;

	const [products] = useApi(async () => {

		const products = await searchApi.faceted({
			query: query,
			pageOffset: 0 as int,
			searchType: ProductSearchType.Reductive,
			pageSize: (pageSize ?? 10) as uint,
			inStockOnly: showInStockOnly,
			includeDynamicBoosts: true,
			hideInactiveProducts: true,
			includePriceStats: false,
			isAdminPanel: false,
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

	}, [query])

	if(!products) {
		return ('');
	}

	if(!products.results || products.results.length == 0) {
		return (
			<Alert variant="info">
				{`No matching products found`}
			</Alert>
		);
	}

	// NB: link has tabindex explicitly set to zero to prevent Safari from losing focus on click
	return (
		<ul className="ui-product-autocomplete">
			{products.results.map(content => {
				const fileRef = content.featureRef;
				return <li>
					<div className="ui-product-autocomplete__item">
						<Quantity product={content}/>
						<Link tabIndex={0}
							href={content.primaryUrl || `/product/${content.slug}`} 
							onClick={() => {
								addToRecentSearches(query);
							}}
						>
							{fileRef && <Image size={32} fileRef={fileRef} className="ui-product-autocomplete__img" />}
							<span className="ui-product-autocomplete__name">
								{content.name}
							</span>
						</Link>
					</div>
				</li>;
			})}
		</ul>
	);
}

export default AutoComplete;