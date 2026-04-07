import Input from 'UI/Input';
import Form from 'UI/Form';
import Button from 'UI/Button';
import Link from 'UI/Link';
import Modal from 'UI/Modal';
import { useState } from 'react';
import { useSession } from 'UI/Session';
import addressApi, { Address } from 'Api/Address';
import CheckoutSection from './CheckoutSection';
import { ApiList } from 'UI/Functions/WebRequest';
import AddressCard from 'UI/Payments/Checkout/AddressCard';
import Col from "UI/Column";
import Row from "UI/Row";
import {toLocaleUTCDateString, isoConvert} from 'UI/Functions/DateTools';

/**
 * Props for the Checkout component.
 */
interface AddressSelectionProps {

	value?: Address;

	setValue: (val: Address) => void;
	setSameAs?: (val: boolean) => void;
	setSavedAddresses: (val: Address[]) => void;

	selectedTitle: string;
	unselectedTitle: string;

	name: string;

	hasSame?: boolean;

	guestCanAdd?: boolean;

	canAdd?: boolean;

	canEdit?: boolean;	

	editUrl?: string;

	isSame?: boolean;

	savedAddresses?: Address[] | undefined;

	addressType: 'delivery' | 'billing';

	enabled: boolean;
}

const AddressSelection: React.FC<AddressSelectionProps> = (props) => {
	const { selectedTitle, unselectedTitle, value, setValue, name, hasSame, isSame,
		setSameAs, setSavedAddresses, addressType, savedAddresses, enabled, guestCanAdd, canAdd, canEdit, editUrl } = props;
	
	const { session } = useSession();		
	const { user } = session;

	const [showNewAddressModal, setShowNewAddressModal] = useState(false);

	const updateAddressFromId = (id: string) => {
		if (savedAddresses) {
			var addressId = parseInt(id,10);
			const addr = savedAddresses.find(a => a.id === addressId);
			if (addr) {
				setValue(addr);
			}
		}
	};

	const addNewAddress = () => {
		return <>
			<Form action={addressApi.create}
				onValues={values => {
					if (addressType == 'delivery') {
						values!.isDefaultDeliveryAddress = true;
					} else if (addressType == 'billing') {
						values!.isDefaultBillingAddress = true;
					}

					if (savedAddresses) { 
						values!.countryCode = savedAddresses[0]?.countryCode || 'GB';
					} else {
						values!.countryCode = 'GB';
					}

					return values;
				}}

				onSuccess={
					(addr: Address) => {

						//date comes back as number from api convert to iso string for consistency with rest of data
						addr.createdUtc = isoConvert(addr.createdUtc).toISOString();
						addr.editedUtc = isoConvert(addr.editedUtc).toISOString();

						if (savedAddresses) {
							const newAddresses = [...savedAddresses, addr].sort((a, b) => (a.name || '').localeCompare(b.name || ''));
							setSavedAddresses(newAddresses);
						}

						setValue(addr);
						setShowNewAddressModal(false);
					}
				}>
				<Input type='text' name='name' label={`Name`} validate={['Required']}/>					
				<Input type='text' name='line1' label={`Address Line 1`} validate={['Required']} />
				<Input type='text' name='line2' label={`Address Line 2`} />
				<Input type='text' name='line3' label={`Address Line 3`} />
				<Input type='text' name='city' label={`City`} validate={['Required']} />
				<Input type='text' name='postcode' label={`Postcode`} validate={['Required']} />

				<div className="payment-checkout__address-modal-footer">
					<Button outlined onClick={() => setShowNewAddressModal(false)}>
						{`Cancel`}
					</Button>
					<Button type="submit">
						{`Add address`}
					</Button>
				</div>
			</Form>
		</>;
	};

    const renderContents = () => {
		return <>
			{hasSame && <div>
				<Input type='checkbox' name={name + '_same'} defaultChecked={isSame} onChange={(e) => {
					setSameAs && setSameAs((e.target as HTMLInputElement).checked);
				}} label={`Same as delivery address`} />
			</div>}
			{(!hasSame || !isSame) && <>

				<Row>
					<Col sizeMd='6' className="payment-checkout__address-selection">
						<div className="payment-checkout__address-selection-selector">
							<Input
								type="select"
								onChange={(e) => updateAddressFromId((e.target as HTMLSelectElement).value)}
								value={value?.id}
								noWrapper
							>

								<option value="" selected={!value?.id}>
									{`Please select a ${addressType} address`}
								</option>

								{savedAddresses?.map(savedAddress => (
									<option
										key={savedAddress.id}
										value={savedAddress.id}
										selected={value?.id === savedAddress.id}
									>
										{savedAddress.name}

										{(savedAddress.city && savedAddress.city.length > 0) && 
											<>{`, ${savedAddress.city}`}</>
										}
										{(savedAddress.postcode && savedAddress.postcode.length > 0) && 
											<>{`, ${savedAddress.postcode}`}</>
										}
									</option>
								))}
							</Input>
						</div>

						<div className="payment-checkout__address-selection-actions">
							{/* Guest can add new address to temp address list */}
							{!user && guestCanAdd && savedAddresses &&  
								<Button onClick={() => setShowNewAddressModal(true)}>
									{`Add new address`}
								</Button>
							}

							{user && canEdit && 
								<Link outlined href={editUrl || "/address_book"}>
									{`Edit addresses`}
								</Link>
							}

							{/* User can add new address which is saved in their address */}
							{user && canAdd && 
								<Button onClick={() => setShowNewAddressModal(true)}>
									{`Add new address`}
								</Button>
							}
						</div>

					</Col>
					<Col sizeMd='6'>
						{value && 
							<div className="payment-checkout__address-selection-selected">
								<AddressCard address={value} selectedAddress={value} name={name} displayName={true} readonly={true} />
							</div>
						}
					</Col>
				</Row>

			</>}

			{showNewAddressModal && <>
				<Modal
					title={`Add Address`}
					className={"payment-checkout__address-modal"}
					onClose={() => setShowNewAddressModal(false)}
					visible={true}>
					{addNewAddress()}
				</Modal>
			</>}

		</>;
	};

	return <CheckoutSection title={value ? selectedTitle : unselectedTitle} enabled={enabled}>
		{renderContents()}
	</CheckoutSection>;
};

export default AddressSelection;