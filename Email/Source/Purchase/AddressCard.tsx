import { Address } from 'Api/Address';
import { emailStyles } from './EmailStyles';

/**
 * Props for the AddressCard component.
 * @icon fal fa-map-marker-alt
 * @description Displays a single address with formatted lines.
 */
interface AddressCardProps {
	address: Address;
}

/**
 * The AddressCard React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const AddressCard: React.FC<AddressCardProps> = (props) => {
	const { address } = props;

	if (!address) {
		return null;
	}

	return (
		<div style={emailStyles.address}>
			{address.line1 && <span style={emailStyles.addressLine}>{address.line1}</span>}
			{address.line2 && <span style={emailStyles.addressLine}>{address.line2}</span>}
			{address.line3 && <span style={emailStyles.addressLine}>{address.line3}</span>}
			{address.city && <span style={emailStyles.addressLine}>{address.city}</span>}
			{address.county && <span style={emailStyles.addressLine}>{address.county}</span>}
			{address.postcode && <span style={emailStyles.addressLine}>{address.postcode}</span>}
		</div>
	);
}

export default AddressCard;