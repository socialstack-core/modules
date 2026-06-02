import getConfig from 'UI/Config';
import {OpayoApi} from 'Api/Payments';
import OpayoExternal from './OpayoExternal';

var _opayo = null; // Lazy loaded opayo API instance.

// Function which ensures opayo is loaded
var ensureLoaded = () => {
	if(_opayo){
		return Promise.resolve(_opayo);
	}else{
		return new Promise((success, reject) => {

			var cfg = getConfig < OpayoConfig > ("Opayo");

			var isEnabled = cfg?.[0]?.isEnabled || false;
			var ownFormEnabled = cfg?.[0]?.ownFormEnabled || false;
            var hostedPageEnabled = cfg?.[0]?.hostedPageEnabled || false;

			if (!isEnabled){
				return;
			}

			if (hostedPageEnabled) {
				// no need for the API if we're only using hosted page, so resolve with an empty object
				_opayo = {};
				success(_opayo);
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

				if (ownFormEnabled) {
					_opayo = opayoOwnForm;
					success(_opayo);
				}
				else
				{
					_opayo = opayoCheckout;
					success(_opayo);
				}
			};

			opayoScript.src = 'https://assets.ci.opayo.cloud/assets/js/opayo-1.2.40.js';
			opayoScript.integrity = 'sha512-ZplJXUTeRh13LTLjfwydrUFpRJaHOoSIKdoMQP4s1gWyJoXqsTqxBeGdt4fQyfNq/Xo21u8IGaA3PjLfiefZJw==';
			opayoScript.crossOrigin = "anonymous";
			document.body.appendChild(opayoScript);
		});
	}
};

var expandYear = (twoDigitYear, pivot = 80) => {
	if (!Number.isInteger(twoDigitYear) || twoDigitYear < 0 || twoDigitYear > 99) {
		throw new RangeError("twoDigitYear must be an integer between 0 and 99");
	}

	const currentYear = new Date().getUTCFullYear();
	const century = Math.floor(currentYear / 100) * 100;

	let year = century + twoDigitYear;
	const upperBound = currentYear + pivot;
	const lowerBound = currentYear - (100 - pivot);

	if (year > upperBound) {
		year -= 100;
	} else if (year < lowerBound) {
		year += 100;
	}

	return year;
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

	// ensure that the colour depth is a valid option
	const colorDepthValues = [1, 4, 8, 15, 16, 24, 32, 48];
	const filtered = colorDepthValues.filter(v => v <= screen.colorDepth);
	const colorDepth = filtered.length > 0 ? Math.max(...filtered) : 24;

    return {
        browserJavascriptEnabled: true,
        browserUserAgent: navigator.userAgent,
		browserLanguage: parsedLanguage,
        browserJavaEnabled: false,
		browserColorDepth: colorDepth,
        browserScreenHeight: screen.height,
        browserScreenWidth: screen.width,
        browserTZ: browserTZ
    };
}

var cfg = getConfig<OpayoConfig>("Opayo");
var isEnabled = cfg?.[0]?.isEnabled || false;

if (isEnabled) {
	// All this module does is force itself into the paymentGateways object.
	var paymentGateways = global.paymentGateways = global.paymentGateways || {};

	paymentGateways.canSaveCards = cfg?.[0]?.canSaveCards || false;
	paymentGateways.ownFormEnabled = cfg?.[0]?.ownFormEnabled || false;
	paymentGateways.hostedPageEnabled = cfg?.[0]?.hostedPageEnabled || false;

	paymentGateways.onGetMerchantKey = () => {
		// Returning a promise will make the card form load until the promise resolves.
		return ensureLoaded().then(() => {
			return OpayoApi.getMerchantSessionKey().then(data => {
				const merchantSessionKey = data.merchantSessionKey;

				return new Promise((success, reject) => {
					success({
						gatewayId: 2, 
						sessionId : merchantSessionKey,
						browserDetails: getBrowserInfo(),
                        component: OpayoExternal
					});
				});

			}).catch(err => {

				return Promise.reject({
					type: 'form_setup_error',
					code: err && err.code ? err.code : 'UnknownError',
					message: err && err.message ? err.message : String(err)
				});
			});
		});
	}

	paymentGateways.onHostedPage = () => {
		// Returning a promise will make the card form load until the promise resolves.
		return ensureLoaded().then(() => {
			return new Promise((success, reject) => {
				success({
					gatewayId: 2, 
					sessionId : 'hosted_page_session', // no session for hosted page
					gatewayToken: 'hosted_page', // no token for hosted page
					browserDetails: getBrowserInfo(),
                    component: OpayoExternal
				});
			});
		});
	}

    paymentGateways.onLoadForm = (key) => {
		return ensureLoaded().then(() => {
			try {
				_opayo({
					merchantSessionKey: key
				}).form();
			} catch (e) {
				console.log(e);
			}
		
		}).catch(err => {
			return Promise.reject({
				type: 'key_error',
				code: err && err.code ? err.code : 'UnknownError',
				message: err && err.message ? err.message : String(err)
			});
		});	
	};

	paymentGateways.onSubmittedCard = cardInfo => {
		
		// Returning a promise will make the card form load until the promise resolves.
		return ensureLoaded().then(() => {
			
			return OpayoApi.getMerchantSessionKey().then(data => {
			
				const merchantSessionKey = data.merchantSessionKey;

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
							// 	"cardIdentifier": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
							// }

							if (result.success) {

								var expiry = new Date(Date.UTC(expandYear(parseInt(paddedYear)), paddedMonth - 1, 1, 0, 0, 0));
								
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

				return Promise.reject({
					type: 'auth_error',
					code: err && err.code ? err.code : 'UnknownError',
					message: err && err.message ? err.message : String(err)
				});
			});
		});
	};
}
