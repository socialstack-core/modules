import Content from 'UI/Content';
import webRequest from 'UI/Functions/WebRequest';
import parseQueryString from 'UI/Functions/ParseQueryString';


//Generally Stripe will either complete or fail, in some circumstances it will be in a pending state
//http://xxxxxxxx?payment_intent=pi_3KHThdBdm0kSBSIC1N1hflQc&payment_intent_client_secret=pi_3KHThdBdm0kSBSIC1N1hflQc_secret_YtwLhMNXDkocDQXsm6oyobaR0&redirect_status=succeeded
export default function StripeComplete(props) {
	var [purchase, setPurchase] = React.useState(null);
	var queryObj = parseQueryString(null, { redirect_status: "failed" });

	//Get the payment intent from querystring
	if (!purchase) {

		webRequest('purchase/list', { where: { thirdPartyId: queryObj.payment_intent } }).then(resp => {
			setPurchase(resp.json.results[0]);
		});
	}

	if (purchase) {
		//if it not exists or the set to fail the set state to error
		//if it exists but is not processed or requires action the display waiting message
		return (
			<div className="stripe-complete">
				<Content type="Purchase" id={purchase.id} live>
					{p => {
						if (p) {

							if (p.didPaymentFail) {
								return <>
									<h1>Payment Failed</h1>
									<p>It looks like your payment has failed.</p>
								</>
							}
							else if (p.doesPaymentRequireAction) {
								return <>
									<h1>Payment Pending</h1>
									<p>It looks like your payment is still processing, please keep this page open until the transaction completes.</p>
								</>
							} else {
								return <>
									<h1>Registration Complete</h1>
									<p>Thank you for registering for the event, Dan will be in contact with further instructions</p>
								</>

							}
						
						} 
					}}
				</Content>
			</div>
		);
	}
	return null;


}


StripeComplete.propTypes = {

};

StripeComplete.defaultProps = {

}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
StripeComplete.icon = 'plus-square';