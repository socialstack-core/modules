import Loading from 'UI/Loading';
import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';
import { useConfig } from 'UI/Session';

function StripeCheckout(props) {
	const { stripe, reactStripe, onSubmit, returnUrl } = props;
	var [loading, setLoading] = React.useState();
	var [failure, setFailure] = React.useState();

	const elements = reactStripe ? reactStripe.useElements() : null;

	const handleSubmit = async (event) => {
		// We don't want to let default form submission happen here,
		// which would refresh the page.
		event.preventDefault();

		onSubmit && onSubmit();

		setLoading(true);
	
		if (!stripe || !elements) {
		  // Stripe.js has not yet loaded.
		  // Make sure to disable form submission until Stripe.js has loaded.
		  return;
		}
	
		const result = await stripe.confirmPayment({
		  //`Elements` instance that was used to create the Payment Element
		  elements: elements,
		  confirmParams: {
			return_url: returnUrl,
		  },
		});

		setLoading(false);
	
		if (result.error) {
		  // Show error to your customer (e.g., payment details incomplete)
		  console.log(result.error.message);
		  setFailure(result.error);
		} else {
		  // Your customer will be redirected to your `return_url`. For some payment
		  // methods like iDEAL, your customer will be redirected to an intermediate
		  // site first to authorize the payment, then redirected to the `return_url`.
		}
	};

	return (
		<form onSubmit={handleSubmit}>
			<reactStripe.PaymentElement />
				<div className="submit-btn">
					<button className="btn btn-primary" disabled={!stripe || loading}>Purchase</button>
				</div>
				{loading && 
					<Loading message="Please wait..." />
				}
				{failure &&
					<div className="stripe-payment-form-error">
						<Alert type="error">{failure.message}</Alert>
					</div>	
				}
		</form>
	);
}

export default function StripePaymentForm(props) {
	const { products, onSubmit, returnUrl, updatePurchaseId } = props;
	var [loading, setLoading] = React.useState();
	var [failure, setFailure] = React.useState();
	var [stripe, setStripe] = React.useState();
	var [previousProducts, setPreviousProducts] = React.useState();
	var [reactStripe, setReactStripe] = React.useState();
	var [clientSecret, setClientSecret] = React.useState();
	var config = useConfig('paymentGateway') || {};
	var publicKey = config.stripePublishableKey;

	React.useEffect(() => {
		var stripeScript = null;
		var existingStripeScript = document.getElementById('Stripe');
		
		if (!existingStripeScript) {
			stripeScript = document.createElement('script');
	
			stripeScript.src = 'https://js.stripe.com/v3/';
			stripeScript.async = true;
			stripeScript.id = name;
			stripeScript.onload = () => {
				if (publicKey && publicKey != "") {
					setStripe(Stripe(publicKey));
				} else {
					setFailure({ message: "Stripe is not configured correctly, there is no public key available" });
				}
			}
		  
		  	document.body.appendChild(stripeScript);
		}

		var reactStripeScript = null;
		var existingReactStripeScript = document.getElementById('ReactStripe');
		
		if (!existingReactStripeScript) {
			reactStripeScript = document.createElement('script');
	
			reactStripeScript.src = 'https://unpkg.com/@stripe/react-stripe-js@latest/dist/react-stripe.umd.min.js';
			reactStripeScript.async = true;
			reactStripeScript.id = name;
			reactStripeScript.onload = () => {
				setReactStripe(ReactStripe);
			}
		  
		  	document.body.appendChild(reactStripeScript);
		}

		return () => {
			if (stripeScript) {
				document.body.removeChild(stripeScript);
			}
			if (reactStripeScript) {
				document.body.removeChild(reactStripeScript);
			}
		}
	}, []);

	React.useEffect(() => {
		if (products && products.length > 0 && products != previousProducts) {
			setLoading(true);

			webRequest("paymentGateway/stripe/create-payment-intent", { 
				products: products.map(product => {
					return {id: product.id, singleCostPence: product.singleCostPence, reccuringCostPence: product.reccuringCostPence};
				}) 
			}).then(response => {
				setClientSecret(response.json.clientSecret);
				updatePurchaseId && updatePurchaseId(response.json.purchaseId);
				setLoading(false);
			}).catch(e => {
				console.log(e);
				if (!e.message) {
					e.message = "Something has gone wrong, please refresh to the page to try again.";
				}
				setLoading(false);
				setFailure(e);
			});
		}

		setPreviousProducts(products);

	}, [products]);

	if (!reactStripe || !stripe || !clientSecret) {
		return (
			<div className="stripe-payment-form">
				<Loading message="Loading Payment Gateway" />
				{failure &&
					<div className="stripe-payment-form-error">
						<Alert type="error">{failure.message}</Alert>
					</div>	
				}
			</div>
		);
	}

	return (
		<div className="stripe-payment-form">
			{reactStripe && stripe && clientSecret && !loading &&
				<reactStripe.Elements stripe={stripe} options={{ clientSecret: clientSecret }}>
					<StripeCheckout stripe={stripe} reactStripe={reactStripe} returnUrl={returnUrl} onSubmit={onSubmit}/>
				</reactStripe.Elements>
			}
			{loading && 
				<Loading message="Please wait..." />
			}
			{failure &&
				<div className="stripe-payment-form-error">
					<Alert type="error">{failure.message}</Alert>
				</div>	
			}
		</div>
	);
}

StripePaymentForm.propTypes = {

};

// use defaultProps to define default values, if required
StripePaymentForm.defaultProps = {

}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
StripePaymentForm.icon='credit-card'; // fontawesome icon
