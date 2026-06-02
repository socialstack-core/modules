import ProductTable, { ExtendedLineItem } from 'UI/Payments/ProductTable';
import { Purchase } from 'Api/Purchase';
import { ProductQuantityPricing } from 'Api/Payments';
import Complete from 'UI/Payments/Complete';

/**
 * Props for the BasicInstruction component.
 */
interface BasicInstructionProps {
	purchase: Purchase
}

/**
 * The BasicInstruction React component.
 * @param props React props.
 */
const View: React.FC<BasicInstructionProps> = (props) => {

	const {
		purchase
	} = props;
	
	// Purchase is a mandatory prop (if trying to view one that can't be seen, the page 404s) but just in case:
	if(purchase == null){
		return `Purchase not found`;
	}
	
    // if coming from checkout, show complete component to handle any status messages
	return <>
        <Complete noSessionUpdate={true} hideUnknownStatus={true} />

		<h1>
			{`Details about your purchase`}
		</h1>
		<ProductTable readOnly shoppingCart={{
			cartContents: ({
				contents: purchase.productQuantities?.map(pq => {
					return (pq as any) as ExtendedLineItem;
				}) || []
			} as any) as ProductQuantityPricing
		}} />
	</>;
}

export default View;