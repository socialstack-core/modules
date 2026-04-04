import { Address } from 'Api/Address';
import AddressCard from './AddressCard';
import { emailStyles } from './EmailStyles';

/**
 * Props for the Purchase Addresses component.
 * @icon fal fa-map-marker-alt
 * @description Displays billing and delivery addresses side by side.
 */
interface AddressesProps {
	billingAddress?: Address;
	deliveryAddress?: Address;
}

/**
 * The Addresses React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const Addresses: React.FC<AddressesProps> = (props) => {
	const { billingAddress, deliveryAddress } = props;

	if (!billingAddress && !deliveryAddress) {
		return null;
	}

	return (
		<div style={emailStyles.container}>
			<table style={emailStyles.table} cellPadding="0" cellSpacing="0" role="presentation">
				<tbody>
					<tr>
						{billingAddress && (
							<td style={emailStyles.cellHalf}>
								<h5 style={emailStyles.heading}>Billing Address</h5>
								<AddressCard address={billingAddress} />
							</td>
						)}

						{deliveryAddress && (
							<td style={emailStyles.cellHalf}>
								<h5 style={emailStyles.heading}>Delivery Address</h5>
								<AddressCard address={deliveryAddress} />
							</td>
						)}
					</tr>
				</tbody>
			</table>
		</div>
	);
};

export default Addresses;