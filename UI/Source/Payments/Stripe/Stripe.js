import getConfig from 'UI/Config';

var _stripe = null; // Lazy loaded stripe API instance.

// Function which ensures stripe is loaded
var ensureLoaded = () => {
	if(_stripe){
		return Promise.resolve(_stripe);
	}else{
		return new Promise((success, reject) => {

			var cfg = getConfig<StripeConfig>("Stripe");
			var publicKey = cfg?.[0]?.publishableKey;
			
			if(!publicKey){
				console.error("No stripe public key defined. It's set in the settings area of your admin panel.");
				return;
			}
			
			var stripeScript = document.createElement('script');
			stripeScript.onload = () => {
				_stripe = global.Stripe;
				_stripe.setPublishableKey(publicKey);
				success(_stripe);
			};
			stripeScript.src = 'https://js.stripe.com/v2/';
			stripeScript.async = true;
			document.body.appendChild(stripeScript);
		});
	}
};

var cfg = getConfig<StripeConfig>("Stripe");
var publicKey = cfg?.[0]?.publishableKey;

if(publicKey && publicKey.length > 0) {
	// All this module does is force itself into the paymentGateways object.
	var paymentGateways = global.paymentGateways = global.paymentGateways || {};

	paymentGateways.onSubmittedCard = cardInfo => {
		
		// Returning a promise will make the card form load until the promise resolves.
		return ensureLoaded().then(() => {
			
			return new Promise((success, reject) => {
				
				_stripe.card.createToken({
					name: cardInfo.name,
					number: cardInfo.number,
					exp_month: cardInfo.exp_month,
					exp_year: cardInfo.exp_year,
					cvc: cardInfo.cvc
				}, (status, response) => {
					if(!response || response.error){
						reject(response.error);
					}else{
						//console.log('Stripe -> Card tokenisation result: ' + JSON.stringify(response));

						var expiry = new Date(Date.UTC(response.card.exp_year, response.card.exp_month - 1, 1, 0, 0, 0));
						
						success({
							last4: response.card.last4, 
							expiry, 
							issuer: cardInfo.issuer, 
							gatewayId: 1, 
							gatewayToken: response.id
						});
					}
				});
			});
			
		});
		
	};
}