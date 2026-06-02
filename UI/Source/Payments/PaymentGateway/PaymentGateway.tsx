import Loading from 'UI/Loading';
import Button from 'UI/Button';
import CardForm from 'UI/Payments/CardForm';
import {isoConvert} from 'UI/Functions/DateTools';
import { useState, useEffect } from 'react';
import paymentMethodApi, { PaymentMethod } from 'Api/PaymentMethod';
import { DefaultInputType } from 'UI/Input/Default';
import { useSession } from 'UI/Session';

type PaymentInputType = DefaultInputType & {
	updateMode?: boolean;
}

// Registering 'payment' as being available
declare global {
	interface InputPropsRegistry {
		'payment': PaymentInputType;
	}
}

/*
* Use this as your main interface for collecting card details via <Input type='payment' name='card' />
* It will vary depending on the gateway(s) available.
*/

function hasCardExpired(expiryUtc : Date) {
	return expiryUtc < new Date();
}

const PaymentGateway: React.FC<CustomInputTypeProps<"payment">> = (props) => {

	const { session } = useSession();
	const { field } = props;

	// If user has saved payment methods, display a dropdown of those or a form to add another one.
	var [methods, setMethods] = useState<PaymentMethod[] | undefined>();
	var [selectedMethod, setSelectedMethod] = useState<PaymentMethod | undefined>();

	useEffect(() => {
		// may be anon user in which case dont attempt to get cards
		if (!props.updateMode && session.user) {
			// Get user's existing validated cards (Returns "non-sensitive" info only).

			paymentMethodApi.list(
				{
					query: "IsValidated=?",
					args: [true],
					pageIndex: 0 as uint,
					pageSize:50 as uint
				}
			).then(response => {
				var methods = response?.results;

				if (methods) {
					methods.sort((a, b) => {
						if (a.lastUsedUtc < b.lastUsedUtc) {
							return 1;
						}
						if (a.lastUsedUtc > b.lastUsedUtc) {
							return -1;
						}
						return 0;
					});

					setMethods(methods);
					setSelectedMethod(methods.length ? methods[0] : undefined);
				}
			});
		}

		// for anon users force a new card
		if (!session.user) {
			setMethods([]);
		}
	}, []);
	
	if (!methods && !field.updateMode){
		return <Loading />;
	}
	
	if(!selectedMethod){
		// Nothing selected.
		if(methods && methods.length){
			return <>
				<select className="form-select"
					onChange={(e) => {
						if(e.target.value != 'none'){
							setSelectedMethod(methods?.find(method => method.id == parseInt(e.target.value)));
						}
					}} value={'none'}>
					<option value='none'>
						{`A new card`}
					</option>
					{methods.map(option => {
						var expiry = '';
						var expiryDate = isoConvert(option.expiryUtc as Date);
						var hasExpired = hasCardExpired(expiryDate);

						if (expiryDate instanceof Date) {
							expiry = new Intl.DateTimeFormat('en-GB', { month: 'numeric', year: '2-digit' }).format(expiryDate);
                        }

						var formattedExpiry = hasExpired ? `expired ${expiry}` : `expires ${expiry}`;

						var isCardDigits = option.name?.length == 4 && !isNaN(parseInt(option.name));
						var name = isCardDigits ? `Card ending ${option.name} (${formattedExpiry})` : option.name;

						return <option value={option.id}
							disabled={hasExpired ? true : undefined}
						>
							{name}
						</option>;
					})}
				</select>
				<CardForm fieldName={field.name}/>
			</>;
		}else{
			// User doesn't have any payment methods at all. Display the new method form.
			return <CardForm fieldName={field.name}/>;
		}
	}
	
	// Otherwise display the selected card.
	return <>
		<CardForm fieldName={field.name} readonly last4={selectedMethod.name} issuer={selectedMethod.issuer} expiry={selectedMethod.expiryUtc} paymentMethodId={selectedMethod.id} />
		<center style={{padding: '1rem'}}>
			<Button variant="secondary" onClick={() => setSelectedMethod(undefined)}>
				{`Use a different card`}
			</Button>
		</center>
	</>;
}

export default PaymentGateway;
window.inputTypes['payment'] = PaymentGateway;