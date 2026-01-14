import useApi from 'UI/Functions/UseApi';
import searchApi, {ProductSearchType, SortDirection} from "Api/ProductSearchController";
import productApi, {Product} from 'Api/Product';
import Link from 'UI/Link';
import Image from 'UI/Image';
import Quantity from 'UI/Product/Quantity';
import Alert from 'UI/Alert';
import Badge from 'UI/Badge';
import { allApprovals } from 'UI/Business/Products/Approval';
import { useSession } from 'UI/Session';
import {addToRecentSearches} from "UI/RecentSearches/SearchHistory";

/**
 * Props for the AutoComplete component.
 */
interface AutoCompleteProps {
	customParameters?: Record<string,any>,
    query:string,
    pageSize?:number,
	showInStockOnly? :boolean
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

    const { session } = useSession();
    var { role} = session;
	
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
			productApi.includes.calculatedprice,
			productApi.includes.businessproductconfig
		]);
		
		return products;

    },[query])

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

	return (
		<ul className="ui-product-autocomplete">
			{products.results.map(content => {
				return <li>
					<div className="ui-product-autocomplete__item">
						<Quantity product={content}/>
						<Link 
							href={content.primaryUrl || `/product/${content.slug}`} 
							onClick={() => {
								addToRecentSearches(query);
							}}
						>
							<Image size={32} fileRef={content.featureRef} className="ui-product-autocomplete__img" />
							<span className="ui-product-autocomplete__name">
								{content.name}
							</span>
							<span className="ui-product-autocomplete__badges">
							</span>
						</Link>
					</div>
				</li>;
			})}
		</ul>
	);
}

export default AutoComplete;