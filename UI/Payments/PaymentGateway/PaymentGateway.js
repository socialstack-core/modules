import Loading from 'UI/Loading';
import CardForm from 'UI/Payments/CardForm';
import webRequest from 'UI/Functions/WebRequest';
import {isoConvert} from 'UI/Functions/DateTools';

/*
* Use this as your main interface for collecting card details via <Input type='payment' name='card' />
* It will vary depending on the gateway(s) available.
*/
var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes.ontypepayment = (props, _this) => {
	return <PaymentGateway {...props} />;
};

function hasCardExpired(expiryUtc) {
	var expiryDate = isoConvert(expiryUtc);

	// treat invalid data as expired
	if (!(expiryDate instanceof Date)) {
		return true;
	}
	
	return expiryDate < new Date();
}

export default function PaymentGateway(props) {
	
	// If user has saved payment methods, display a dropdown of those or a form to add another one.
	var [methods, setMethods] = React.useState();
	var [selectedMethod, setSelectedMethod] = React.useState();

	React.useEffect(() => {
		if(!props.updateMode) {
			// Get user's existing cards (Returns "non-sensitive" info only).
			webRequest('paymentmethod/list').then(response => {

				var methods = response.json.results;
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
				setSelectedMethod(methods.length ? methods[0] : null);
			});
		}
	}, []);
	
	if(!methods && !props.updateMode){
		return <Loading />;
	}
	
	if(!selectedMethod){
		// Nothing selected.
		if(methods && methods.length){
			return <>
				<select className="form-select"
					onChange={(e) => {
						if(e.target.value != 'none'){
							setSelectedMethod(methods.find(method => method.id == e.target.value));
						}
					}} value={'none'}>
					<option value='none'>
						{`A new card`}
					</option>
					{methods.map(option => {
						var expiry = '';
						var expiryDate = isoConvert(option.expiryUtc);
						var hasExpired = hasCardExpired(option.expiryUtc);

						if (expiryDate instanceof Date) {
							expiry = new Intl.DateTimeFormat('en-GB', { month: 'numeric', year: '2-digit' }).format(expiryDate);
                        }

						var formattedExpiry = hasExpired ? `expired ${expiry}` : `expires ${expiry}`;

						var isCardDigits = option.name.length == 4 && !isNaN(option.name);
						var name = isCardDigits ? `Card ending ${option.name} (${formattedExpiry})` : option.name;

						return <option value={option.id}
							disabled={hasExpired ? 'disabled' : undefined}
						>
							{name}
						</option>;
					})}
				</select>
				<CardForm fieldName={props.name}/>
			</>;
		}else{
			// User doesn't have any payment methods at all. Display the new method form.
			return <CardForm fieldName={props.name}/>;
		}
	}
	
	// Otherwise display the selected card.
	return <>
		<CardForm fieldName={props.name} readonly last4={selectedMethod.name} issuer={selectedMethod.issuer} expiry={selectedMethod.expiryUtc} paymentMethodId={selectedMethod.id} />
		<center style={{padding: '1rem'}}>
			<button onClick={() => {
				setSelectedMethod(null);
			}} className="btn btn-secondary">
				Use a different card
			</button>
		</center>
	</>;
}

