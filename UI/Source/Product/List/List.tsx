import { Product } from "Api/Product";
import Signpost from 'UI/Product/Signpost';

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
	viewStyle?: string

	/**
	 * Adds a paginator to the product list.
	 */
	paginator?: React.ReactElement,

	/**
	 * Append UI to the product items
	 */
	productExtras?: (React.FC | React.FC<unknown>)[],
}

/**
 * The List React component.
 * @param props React props.
 */
const List: React.FC<ListProps> = (props: ListProps) => {
	const { content, viewStyle } = props;

	if (!content) {
		return null;
	}

	let productListClasses = ["ui-product-list"];
	productListClasses.push(`ui-product-list--${viewStyle}`);

	return <>
		<div className="ui-product-list__wrapper">
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