import { Address } from 'Api/Address';
import Collapsible from 'UI/Collapsible';
import AddressCard from 'UI/Payments/Checkout/AddressCard';

/**
 * Props for the Purchase Addresses component.
 */
interface AddressesProps {
	billingAddress?: Address;
    deliveryAddress?: Address;
}

const Addresses: React.FC<AddressesProps> = (props) => {
	const { billingAddress, deliveryAddress} = props;

	if (!billingAddress && !deliveryAddress) {
		return ('');
	}

	return <>
			<Collapsible title={`Addresses`} open={true}>
			<div className="payment-checkout__address-selection">
				{billingAddress && 
					<div className="purchase-addresses__address">
						<h5>{`Billing Address`}</h5>
						<AddressCard address={billingAddress} readonly />
					</div>
				}

				{deliveryAddress &&
					<div className="purchase-addresses__address">
						<h5>{`Delivery Address`}</h5>
						<AddressCard address={deliveryAddress} readonly/>
					</div>
				}
			</div>
			</Collapsible>
	</>;
};

export default Addresses