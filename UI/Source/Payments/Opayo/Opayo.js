import getConfig from 'UI/Config';
import OpayoController from 'Api/OpayoController';

var _opayo = null; // Lazy loaded opayo API instance.

// Function which ensures opayo is loaded
var ensureLoaded = () => {
	if(_opayo){
		return Promise.resolve(_opayo);
	}else{
		return new Promise((success, reject) => {

			var cfg = getConfig<OpayoConfig>("Opayo");
			var integrationKey = cfg?.[0]?.integrationKey;

			if(!integrationKey){
				console.error("No opayo integration key defined. It's set in the settings area of your admin panel.");
				return;
			}

			var restHost = cfg?.[0]?.restHost;
			var restBaseURL = cfg?.[0]?.restBaseURL;

			var opayoScript = document.createElement('script');
			
			opayoScript.onload = () => {
				// only set for testing / sandboxing
				if (restHost && restHost.length > 0) {
					OpayoConfig.restHost = restHost;
				}
				
				// only set for testing / sandboxing
				if (restBaseURL && restBaseURL.length > 0) {
					OpayoConfig.restBaseURL = restBaseURL;
				}

				_opayo = opayoOwnForm;
				success(_opayo);
			};

			opayoScript.src = 'https://assets.ci.opayo.cloud/assets/js/opayo-1.2.40.js';
			opayoScript.integrity = 'sha512-ZplJXUTeRh13LTLjfwydrUFpRJaHOoSIKdoMQP4s1gWyJoXqsTqxBeGdt4fQyfNq/Xo21u8IGaA3PjLfiefZJw==';
			opayoScript.crossOrigin = "anonymous";
			document.body.appendChild(opayoScript);
		});
	}
};

var getBrowserInfo = () => {
    // Timezone in ±HHMM format
    let offset = -new Date().getTimezoneOffset(); 
    const sign = offset >= 0 ? "+" : "-";
    offset = Math.abs(offset);

    const hours = String(Math.floor(offset / 60)).padStart(2, "0");
    const minutes = String(offset % 60).padStart(2, "0");
	const browserTZ = `${sign}${hours}${minutes}`;

	const localeLanguage = (navigator.language || 'en-GB').replace('_', '-');
	const matchedLanguage = localeLanguage.match(/^[a-zA-Z]{2}(?:-[a-zA-Z]{2})?/);
	const parsedLanguage = matchedLanguage ? matchedLanguage[0] : 'en-GB';

    return {
        browserJavascriptEnabled: true,
        browserUserAgent: navigator.userAgent,
		browserLanguage: parsedLanguage,
        browserJavaEnabled: false,
        browserColorDepth: screen.colorDepth,
        browserScreenHeight: screen.height,
        browserScreenWidth: screen.width,
        browserTZ: browserTZ
    };
}

var cfg = getConfig<OpayoConfig>("Opayo");
var integrationKey = cfg?.[0]?.integrationKey;

if(integrationKey && integrationKey.length > 0) {
	// All this module does is force itself into the paymentGateways object.
	var paymentGateways = global.paymentGateways = global.paymentGateways || {};

	paymentGateways.onSubmittedCard = cardInfo => {
		
		// Returning a promise will make the card form load until the promise resolves.
		return ensureLoaded().then(() => {
			
			return OpayoController.getMerchantSessionKey().then(data => {
			
				const merchantSessionKey = data.merchantSessionKey;
				//console.log('Opayo -> Obtained merchant session key', data,cardInfo);

				return new Promise((success, reject) => {

					var paddedMonth = String(cardInfo.exp_month).padStart(2, '0');
					var paddedYear = String(cardInfo.exp_year).padStart(2, '0'); 		
					var cardNumber = cardInfo.number.replace(/\s+/g, '');			
					
					_opayo({
						merchantSessionKey: merchantSessionKey
					}).tokeniseCardDetails({
						cardDetails: {
							cardholderName: cardInfo.name,
							cardNumber: cardNumber,
							expiryDate: paddedMonth + paddedYear,
							securityCode: cardInfo.cvc
						},
						onTokenised: function(result) {
							// {
							// 	"cardIdentifier": "8478F5AB-DA8B-40DF-B30D-FA9B6A1BA886",
							// 	"expiry": "2025-11-26T16:04:25.412Z",
							// 	"cardType": "Visa"
							// }
							// console.log('Opayo -> Card tokenisation result: ' + JSON.stringify(result));

							if (result.success) {
								var expiry = new Date(result.expiry);
								
								success({
									last4: cardInfo.number.slice(-4), 
									expiry, 
									issuer: cardInfo.issuer, 
									gatewayId: 2, 
									gatewayToken: result.cardIdentifier,
                                    sessionId : merchantSessionKey,
                                    browserDetails: getBrowserInfo()
								});

							} else {
								// {
								// 	"errors": [
								// 		{
								// 			"description": "The card number failed our validity checks and is invalid",
								// 			"property": "cardDetails.cardNumber",
								// 			"clientMessage": "The card number is invalid",
								// 			"code": 1007
								// 		}
								// 	]
								// }

								// console.log('Error tokenising card details: ' + JSON.stringify(result));

								if (!result.errors || !result.errors.length) {
									reject({
										type: 'UnknownError',
										message: 'An unknown error occurred'
									});
								}
								
								reject({
									type: "card_error",
									code: result.errors[0].code,
									message: result.errors[0].message
								});
							}
						}
					});

				});
			}).catch(err => {
				console.log('Error tokenising card details : ' + JSON.stringify(err));

				return Promise.reject({
					type: 'auth_error',
					code: err && err.code ? err.code : 'UnknownError',
					message: err && err.message ? err.message : String(err)
				});
			});
		});
	};
}
