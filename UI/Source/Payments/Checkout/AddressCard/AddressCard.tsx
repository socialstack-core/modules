import Input from 'UI/Input';
import { Address } from 'Api/Address';


/**
 * Props for the AddressCard component.
 */
interface AddressCardProps {
	address: Address,
	selectedAddress?: Address,
	onChange: Function,
	name: string,
    readonly: boolean
}

/**
 * The AddressCard React component.
 * @param props React props.
 */
const AddressCard: React.FC<AddressCardProps> = (props) => {
	var { address, selectedAddress, onChange, name , readonly} = props;

	if (!address) {
		return;
	}

	return <>
		{readonly ? 
			<address className="payment-checkout__address payment-checkout__address__readonly">
				{address.line1 && <span>{address.line1}</span>}
				{address.line2 && <span>{address.line2}</span>}
				{address.line3 && <span>{address.line3}</span>}
				{address.city && <span>{address.city}</span>}
				{address.county && <span>{address.county}</span>}
				{address.postcode && <span>{address.postcode}</span>}
			</address>
		:
		<Input type="radio" name={name} value={address.id.toString()} checked={selectedAddress?.id == address.id} noWrapper
			onChange={onChange}
			label={<>
				<address className="payment-checkout__address">
					{address.line1 && <span>{address.line1}</span>}
					{address.line2 && <span>{address.line2}</span>}
					{address.line3 && <span>{address.line3}</span>}
					{address.city && <span>{address.city}</span>}
					{address.county && <span>{address.county}</span>}
					{address.postcode && <span>{address.postcode}</span>}
				</address>
			</>} />
		}
	</>;
}

export default AddressCard;