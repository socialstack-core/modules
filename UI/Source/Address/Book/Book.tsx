import addressApi, { Address } from 'Api/Address';
import useApi from 'UI/Functions/UseApi';
import { useSession } from 'UI/Session';
import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';
import { useState } from 'react';
import Button from 'UI/Button';
import ConfirmModal from 'UI/Modal/ConfirmModal';

/**
 * Props for the Address/Book component.
 */
interface BookProps {
	addressType?: uint
	businessId? :uint
}

/**
 * The Address Book React component.
 * @param props React props.
 */
const Book: React.FC<BookProps> = (props: BookProps) => {
	const { session } = useSession();
	const { user } = session;

	// A counter which is used to cause the address list to update with new entries.
	const [createCounter, setCreateCounter] = useState<number>(0);
	const [confirmDelete, setConfirmDelete] = useState<Address | null>(null);

	var addressType = props.addressType === undefined ? 0 : props.addressType;

	// permissions handle any filtering
	var addressQuery: any = {
		query : 'AddressType=?',
		args : [
			addressType
		],
		sort: null
	}

	const [ addressList ] = useApi(() => addressApi.list(addressQuery), [createCounter]);

	if (!user) {
		// Page login rules block this, but just in case.
		return null;
	}

	if (!addressList) {
		return <Loading />;
	}

	const addressToLines = (addr: Address) => {
		var current = [];
		addr.line1 && current.push(addr.line1);
		addr.line2 && current.push(addr.line2);
		addr.line3 && current.push(addr.line3);
		addr.city && current.push(addr.city);
		addr.postcode && current.push(addr.postcode);
		return current;
	};

	return (
		<div className="ui-address-book">
			<h1 className="ui-address-book__title">
				{`Edit addresses`}
			</h1>
			<ul className="ui-address-book__list">
				{addressList.results.map((address: Address) => {
					return <li className="ui-address-book__item">
						<header className="ui-address-book__item-header">
							{address.isDefaultBillingAddress && <>
								<span className="ui-address-book__item-default">
									<i className="fr fr-check-circle"></i>
									{`Default billing address`}
								</span>
							</>}
							{address.isDefaultDeliveryAddress && <>
								<span className="ui-address-book__item-default">
									<i className="fr fr-check-circle"></i>
									{`Default delivery address`}
								</span>
							</>}
						</header>
						{addressToLines(address).map(line => <div>{line}</div>)}
						<footer className="ui-address-book__item-footer">
							{/* TODO */}
							{/*<Button onClick={() => ...}>{`Edit`}</Button>*/}
							<Button onClick={() => setConfirmDelete(address)}>{`Delete`}</Button>
						</footer>
					</li>

				})}
			</ul>

			<section className="ui-address-book__add">
				<h2 className="ui-address-book__subtitle">
					{`Add new address`}
				</h2>
				<Form action={addressApi.create} submitLabel={`Add address`}
				onValues={values => {
					values.addressType = addressType;
					return values;
				}}

				onSuccess={
					() => setCreateCounter(createCounter+1)
				}>
					<Input type='text' name='line1' label={`Address Line 1`} />
					<Input type='text' name='line2' label={`Address Line 2`} />
					<Input type='text' name='line3' label={`Address Line 3`} />
					<Input type='text' name='city' label={`City`} />
					<Input type='text' name='postcode' label={`Postcode`} />
					<Input type='checkbox' name='isDefaultBillingAddress' label={`Set as default billing address`} />
					<Input type='checkbox' name='IsDefaultDeliveryAddress' label={`Set as default shipping address`} />
				</Form>
			</section>
			{
				confirmDelete && <ConfirmModal
					confirmVariant={'danger'}
					confirmCallback={() => {
						return addressApi.delete(confirmDelete.id).then(() => {
							setConfirmDelete(null);
							setCreateCounter(createCounter + 1);
						});
					}}
					cancelCallback={() => setConfirmDelete(null)}
					confirmText={`Yes, delete the address`}
				>
					{`Are you sure you want to delete this address?`}
				</ConfirmModal>
			}
		</div>
	);
}

export default Book;