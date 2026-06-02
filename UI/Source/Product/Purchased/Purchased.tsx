import { ProductStat } from 'Api/BusinessProductStats';
import { Product } from 'Api/Product';
import Time from 'UI/Time';

/**
 * Props for the Purchase component.
 */
interface PurchaseProps {
	/**
	 * The product to display
	 */
	product: Product
}

/**
 * The Purchase React component, this needs an include for BusinessProductStats on the product
 * @param props React props.
 */
const Purchase: React.FC<PurchaseProps> = (props) => {
	const { product } = props;

	const stats:ProductStat | undefined = (product?.businessProductStats || undefined);

    if (!stats) {
        return ('');
    }

	// todo - add popup with breakdown 

	return <div className="ui-product-purchased">
		<div className="ui-product-purchased__wrapper">
			{`You last purchased this `}<Time date={stats.lastOrderedUtc} />
		</div>
	</div>;
}

export default Purchase;