import { Product } from "Api/Product";
import { ProductCategory } from 'Api/ProductCategory';
import Signpost from 'UI/Product/Signpost';
import PromotionCycler, {InlinePromotion} from 'UI/PromotionCycler';

/**
 * Props for the List component.
 */
interface ListProps {
	/**
	 * The products to list. Must have included 'primaryUrl'.
	 */
	content?: Product[],

	/** 
	 * determines if products should be shown in small thumbnail, large thumbnail or grid format
	 */
	viewStyle?: "list" | "small-thumbs" | "large-thumbs",

	/**
	 * Adds a paginator to the product list.
	 */
	paginator?: React.ReactElement,

	/**
	 * Append UI to the product items
	 */
	productExtras?: (React.FC | React.FC<unknown>)[],

	/**
	 * Promotions to display inline in the list (category placements)
	 */
	promotions?: InlinePromotion[],

	/**
	 * Current category ID for condition filtering
	 */
	currentCategoryId?: number,

	/**
	 * Current category breadcrumbs for includeChildren condition filtering
	 */
	currentCategoryBreadcrumbs?: ProductCategory[],
}

/**
 * The List React component.
 * @param props React props.
 */
const List: React.FC<ListProps> = (props: ListProps) => {
	const { content, viewStyle, promotions, currentCategoryId, currentCategoryBreadcrumbs } = props;

	if (!content) {
		return null;
	}

	let productListClasses = ["ui-product-list"];
	productListClasses.push(`ui-product-list--${viewStyle}`);

	return <>
		<div className="ui-product-list__wrapper">
			{promotions && promotions.length > 0 && (
				<div className="ui-product-list__promotion" style={{ marginBottom: '1rem' }}>
					<PromotionCycler promotions={promotions} currentCategoryId={currentCategoryId as int} currentSearchCategoryId={currentCategoryId as int}
						currentCategoryBreadcrumbs={currentCategoryBreadcrumbs} />
				</div>
			)}
			<ul className={productListClasses.join(' ')}>
				{content.map(product => {
					return <li className="ui-product-list__product">
						<Signpost 
							// ================================
							// Signpost "after" injection
							// --------------------------------
							// This displays the supplied 
							// components at the end of the 
							// Signposts container.
							// --------------------------------
							after={props?.productExtras} 
							// ================================
							content={product} 
							viewStyle={viewStyle} 
						/>
					</li>;
				})}
			</ul>
			{props.paginator}
		</div>
	</>;
}

export default List;