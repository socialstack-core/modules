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

	canAdd?: boolean;

	editUrl?: string;

	isSame?: boolean;

	savedAddresses?: Address[] | undefined;

	addressType: 'delivery' | 'billing';

	enabled: boolean;
}

const AddressSelection: React.FC<AddressSelectionProps> = (props) => {
	const { selectedTitle, unselectedTitle, value, setValue, name, hasSame, isSame,
		setSameAs, setSavedAddresses, addressType, savedAddresses, enabled, canAdd, editUrl } = props;
	
	const { session } = useSession();		
	const { user } = session;

    const [showEditGuestAddressModal, setShowEditGuestAddressModal] = useState(false);
	const [showNewAddressModal, setShowNewAddressModal] = useState(false);

	const canEdit = !!savedAddresses;

	let canAddIntl = canEdit ? canAdd : true;

	const addNewAddress = () => {
		return <>
			<Form action={addressApi.create}
				onValues={values => {
					if (addressType == 'delivery') {
						values!.isDefaultDeliveryAddress = true;
					} else if (addressType == 'billing') {
						values!.isDefaultBillingAddress = true;
					}

					return values;
				}}

				onSuccess={
					(addr: Address) => {
						setValue(addr);
						setShowNewAddressModal(false);
					}
				}>
				<Input type='text' name='line1' label={`Address Line 1`} />
				<Input type='text' name='line2' label={`Address Line 2`} />
				<Input type='text' name='line3' label={`Address Line 3`} />
				<Input type='text' name='city' label={`City`} />
				<Input type='text' name='postcode' label={`Postcode`} />

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

	// Allow the editing of the address inline for guests etc without an address book
	const editGuestAddress = () => {
		return <>
			<Form action={async (fields) => fields}
				onValues={values => {
					return values;
				}}

				onSuccess={
					addr => {
						if (savedAddresses) {
							// merge the changes into the saved address
							const index = savedAddresses?.findIndex(a => a.id === value?.id);
							if (index !== -1 && index !== undefined) {

								const updated = {
									...savedAddresses[index],
									...addr,
								};

								const newAddresses = savedAddresses.map((a, i) =>
									i === index ? updated : a
								);

								// notify parent
								setSavedAddresses(newAddresses);
								setValue(savedAddresses[index]);
							}
						}

						setShowEditGuestAddressModal(false);
					}
				}>
				<Input type='text' name='name' label={`Name`} defaultValue={value?.name} />
				<Input type='text' name='line1' label={`Address Line 1`} defaultValue={value?.line1} />
				<Input type='text' name='line2' label={`Address Line 2`} defaultValue={value?.line2} />
				<Input type='text' name='line3' label={`Address Line 3`} defaultValue={value?.line3} />
				<Input type='text' name='city' label={`City`} defaultValue={value?.city} />
				<Input type='text' name='postcode' label={`Postcode`} defaultValue={value?.postcode} />
				
				<div className="payment-checkout__address-modal-footer">
					<Button outlined onClick={() => setShowEditGuestAddressModal(false)}>
						{`Cancel`}
					</Button>
					<Button type="submit">
						{`Save Changes`}
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
				<div className="payment-checkout__address-selection">
                    {value && 
                        <AddressCard address={value} selectedAddress={value} name={name} />
                    }

					{savedAddresses?.filter(a => a.id !== value?.id)
						.map(savedAddress => (
							<AddressCard
								key={savedAddress.id}
								address={savedAddress}
								selectedAddress={value}
								onChange={() => setValue(savedAddress)}
								name={name}
						/>
					))}

				</div>
				<div className="payment-checkout__address-selection-footer">
					{user && canEdit && <Link outlined href={editUrl || "/address_book"}>
						{`Edit addresses`}
					</Link>}

					{!user && canEdit && <Button onClick={() => setShowEditGuestAddressModal(true)}>
						{`Edit address`}
					</Button>}

					{canAddIntl && <Button onClick={() => setShowNewAddressModal(true)}>
						{`Add new address`}
					</Button>}
				</div>
			</>}

			{showNewAddressModal && <>
				<Modal
					title={`Add address`}
					className={"payment-checkout__address-modal"}
					onClose={() => setShowNewAddressModal(false)}
					visible={true}>
					{addNewAddress()}
				</Modal>
			</>}

			{showEditGuestAddressModal && <>
				<Modal
					title={`Edit address`}
					className={"payment-checkout__address-modal"}
					onClose={() => setShowEditGuestAddressModal(false)}
					visible={true}>
					{editGuestAddress()}
				</Modal>
			</>}


		</>;
	};

	return <CheckoutSection title={value ? selectedTitle : unselectedTitle} enabled={enabled}>
		{renderContents()}
	</CheckoutSection>;
};

export default AddressSelection;