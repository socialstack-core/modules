import ProductTable from 'UI/Payments/ProductTable';
import Input from 'UI/Input';
import Button from 'UI/Button';
import Link from 'UI/Link';
import Form from 'UI/Form';
import Alert from 'UI/Alert';
import Loading from 'UI/Loading';
import { useState, useEffect, useRef } from 'react';
import { useSession } from 'UI/Session';
import { useRouter } from 'UI/Router';
import useApi from 'UI/Functions/UseApi';
import { useCart } from 'UI/Payments/CartSession';
import shoppingCartApi, { GuestDetails, PurchaseAndAction} from 'Api/ShoppingCart';
import deliveryOptionApi, { CartEstimation, DeliveryOption } from 'Api/DeliveryOption';
import addressApi, { Address } from 'Api/Address';
import { PurchaseStatus } from 'Api/Purchase';
import { ApiList } from 'UI/Functions/WebRequest';
import AddressSelection from './AddressSelection';
import CheckoutSection from './CheckoutSection';
import ChallengeChecker from 'UI/Payments/Approval/ChallengeChecker';
import Challenge from 'UI/Payments/Approval/Challenge';
import Modal from 'UI/Modal';
import DeliveryOptions from './DeliveryOptions';
import ExternalPayment from 'UI/Payments/ExternalPayment';

/**
 * Props for the Checkout component.
 */
interface CheckoutProps {
	canPayLater: (session:Session) => boolean;
}

/**
 * The Cart React component.
 * @param props React props.
 */
const Checkout: React.FC<CheckoutProps> = (props) => {
	const { canPayLater } = props;
	const { session } = useSession();
	const { user ,locale} = session;
	const { setPage, pageState } = useRouter();
	const { query } = pageState;
	var { shoppingCart, cartIsEmpty, emptyCart, lessTax, getCartId, cartIsDigitalOnly, addGuestDetails } = useCart();

	const ssFormRef = useRef<HTMLFormElement>(null);

	// Does the cart contain any physical products or not?
	const downloadsOnly = cartIsDigitalOnly ? cartIsDigitalOnly() : false;

	const deferPayment = user ? (canPayLater ? canPayLater(session) : false) : false;
	const paymentRequired = ((user && !deferPayment) || !user)
	
	const [deliveryAddress, setDeliveryAddress] = useState<Address | undefined>();
	const [sameAsDelivery, setSameAsDelivery] = useState<boolean>(true);
	const [billingAddress, setBillingAddress] = useState<Address | undefined>();
    const [savedAddresses, setSavedAddresses] = useState<Address[] | undefined>();
	
	const [deliveryOption, setDeliveryOption] = useState<DeliveryOption | undefined>();
	const [estimates, setEstimates] = useState<ApiList<DeliveryOption> | undefined>();

	const [challengeMetaData , setChallengeMetaData] = useState<PurchaseAndAction | undefined>();

	const [acceptedTerms, setAcceptedTerms] = useState<boolean>(user ? true : false);
	const [acceptedPrivacy, setAcceptedPrivacy] = useState<boolean>(user ? true : false);
	
	const [customerOrderReference, setCustomerOrderReference] = useState<string | undefined>(undefined);
	const [contactName, setContactName] = useState<string | undefined>(undefined);
	const [deliveryInformation, setDeliveryInformation] = useState<string | undefined>(undefined);

	enum CheckoutStep {
		OrderContents,
		DeliveryAddress,
		BillingAddress,
		DeliveryDate,
		PaymentMethod,
		TermsConditions,
		TransactionApproval
	}

	let currentStep: CheckoutStep = CheckoutStep.DeliveryAddress;

	if (!!deliveryAddress) {
		currentStep = CheckoutStep.BillingAddress;

		if (sameAsDelivery || !!billingAddress) {
			currentStep = CheckoutStep.DeliveryDate;

			if (!!deliveryOption || downloadsOnly) {
				currentStep = CheckoutStep.PaymentMethod;

				if (!!challengeMetaData) {
					currentStep = CheckoutStep.TermsConditions;
				} 
				else 
				{
					currentStep = CheckoutStep.TransactionApproval;
				}
			}
		}
	}

	const findDefaultAddress = (
	addresses: Address[],
	addressType: 'delivery' | 'billing'
	): Address | undefined => {
		const flag = addressType === 'delivery' ? 'isDefaultDeliveryAddress' : 'isDefaultBillingAddress';

		return addresses
			.filter(a => a[flag] && Number.isFinite(Number(a.id)))
			.sort((a, b) => Number(b.id) - Number(a.id))[0];
	};

	// Gets the billing and delivery addresses for a user 
	useApi(() => !user ? Promise.resolve(undefined) : addressApi
		.getCartAddresses()
		.then(addrSet => {
			setSavedAddresses(addrSet.results.sort((a, b) => (a.name || '').localeCompare(b.name || '')));

			const deliveryAddr = findDefaultAddress(addrSet.results, 'delivery');
			const billingAddr = findDefaultAddress(addrSet.results, 'billing');
			
			setDeliveryAddress(deliveryAddr);
			setBillingAddress(billingAddr);

			if(deliveryAddr?.contact?.trim()) 
			{
				setContactName(deliveryAddr.contact);
			} 
			else if (billingAddr?.contact?.trim()) {
				setContactName(billingAddr.contact);
			}

			if (billingAddr) {
				setSameAsDelivery((billingAddr.anonKey == deliveryAddr?.anonKey));
			} else {
				setSameAsDelivery(true);
			}

		}
	), []);
	
	// Gets the billing and delivery addresses for guests
	useEffect(() => {

		if (!shoppingCart || !shoppingCart?.guestDetailsJson || shoppingCart.guestDetailsJson.length === 0 ) {
			return;
		}

		var guestDetails = JSON.parse(shoppingCart.guestDetailsJson);

		if(!guestDetails || !guestDetails.addresses || guestDetails.addresses.length === 0){
			return;
		}

		var deliveryAddr : Address | undefined;

		if (shoppingCart.deliveryAddressId > 0) {
			deliveryAddr = guestDetails.addresses.find((addr: Address) => addr.id == shoppingCart.deliveryAddressId);
			setDeliveryAddress(deliveryAddr);
		} else {
			deliveryAddr = findDefaultAddress(guestDetails.addresses, 'delivery');
			setDeliveryAddress(deliveryAddr);
		}
		
		var billingAddr : Address | undefined;

		if (shoppingCart.billingAddressId > 0) {
			billingAddr = guestDetails.addresses.find((addr: Address) => addr.id == shoppingCart.billingAddressId);
			setBillingAddress(billingAddr);
		} else {
			billingAddr = findDefaultAddress(guestDetails.addresses, 'billing');
			setBillingAddress(billingAddr);
		}

		setSavedAddresses(guestDetails.addresses.sort((a, b) => (a.name || '').localeCompare(b.name || '')));

		if(deliveryAddr?.contact?.trim()) 
		{
			setContactName(deliveryAddr.contact);
		} 
		else if (billingAddr?.contact?.trim()) {
			setContactName(billingAddr.contact);
		}

		if (billingAddr) {
			setSameAsDelivery((billingAddr.anonKey == deliveryAddr?.anonKey));
		} else {
			setSameAsDelivery(true);
		}		
	}, [shoppingCart]);	
	
		
	// Load delivery options for this cart
	useEffect(() => {
		var cartRef = getCartId!();
		
		if (!deliveryAddress || downloadsOnly) {
			return;
		}

		let cartEstimation:CartEstimation = {};

		cartEstimation.deliveryAddressKey = deliveryAddress.anonKey; 

		deliveryOptionApi.estimate(cartRef.id, cartRef.anonKey, cartEstimation).then(estimates => {
			if (estimates.results.length > 0) {
				// Pick the first one by default always:
				setDeliveryOption(estimates.results[0]);
			}

			setEstimates(estimates);
		});
		
	}, [deliveryAddress]);
	
	// Update in cart delivery address after change/edit for guests 
	useEffect(() => {
		if(user || !addGuestDetails || !deliveryAddress || !savedAddresses) {
			return;
		}

		if (!shoppingCart || !shoppingCart?.guestDetailsJson || shoppingCart.guestDetailsJson.length === 0 ) {
			return;
		}

		var guestDetails:GuestDetails = JSON.parse(shoppingCart.guestDetailsJson);

		if(!guestDetails || !guestDetails.addresses || guestDetails.addresses.length === 0) {
			return;
		}

		const areAddressesEqual = JSON.stringify(guestDetails.addresses) === JSON.stringify(savedAddresses);
		if(areAddressesEqual) {
			return;
		}

		guestDetails.addresses = savedAddresses;
		
		// save the updated guest details into the cart
		addGuestDetails(guestDetails).then(() => {
			// continue to guest checkout
		}).catch(e => {
			console.log('failed to add guest details to cart', e);
		});

	}, [savedAddresses]);

	if (user && !savedAddresses) {
		// Addresses or delivery options currently loading
		return <div className="payment-checkout">
			<Loading />
		</div>;
	}

	if (!cartIsEmpty || cartIsEmpty()) {
		return <div className="payment-checkout">
			<Alert type='info'>
				{`Your cart is currently empty`}
			</Alert>
		</div>;
	}

	return <>
		<div className="payment-checkout">
			<h1 className="payment-checkout__title">
				{`Checkout`}
			</h1>
			<ol className="payment-checkout__steps">
				<li>
					{/* order contents */}
					<CheckoutSection title={`Order contents`} enabled={true}>
                        <ProductTable tableFormat={true} shoppingCart={shoppingCart} readOnly lessTax={lessTax} />                        
					</CheckoutSection>
				</li>

				<li>
					{/* delivery address */}
					<AddressSelection selectedTitle={`Delivering to`} unselectedTitle={`Select a delivery address`}
						name='delivery' canAdd={true} guestCanAdd={true} canEdit={true} savedAddresses={savedAddresses} 
						value={deliveryAddress} setValue={setDeliveryAddress} setSavedAddresses={setSavedAddresses}addressType='delivery'
						enabled={true}
					/>
				</li>

				<li className={currentStep < CheckoutStep.BillingAddress ? "payment-checkout__step--disabled" : ""}>
					{/* billing currentStep */}
					<AddressSelection selectedTitle={`Billing Address`} unselectedTitle={`Select a billing address`}
						name='billing' savedAddresses={savedAddresses}
						value={billingAddress} setValue={setBillingAddress} setSavedAddresses={setSavedAddresses}	
						hasSame={true} isSame={sameAsDelivery} setSameAs={setSameAsDelivery} addressType='billing'
						enabled={currentStep >= CheckoutStep.BillingAddress}
					/>
				</li>

				<li className={currentStep < CheckoutStep.DeliveryDate ? "payment-checkout__step--disabled" : ""}>
					{/* delivery date */}
					<CheckoutSection title={`Delivery`} enabled={currentStep >= CheckoutStep.DeliveryDate}>
						{estimates ? 
							<DeliveryOptions 
								estimates={estimates} 
								shoppingCart={shoppingCart} 
								locale={locale} 
								deliveryOption={deliveryOption} 
								setDeliveryOption={setDeliveryOption}
								deliveryInformation={deliveryInformation}
								setDeliveryInformation={setDeliveryInformation}								
							/> 
						: 
							<Loading />
						}
					</CheckoutSection>
				</li>

				<li>
					<CheckoutSection title={`Additional Information`} enabled={true}>
						<Input type="text" 
							label={`Contact Name`}
							validate={['Required']}
							defaultValue={contactName}
							onChange={e => {
								const input = (e.target as HTMLInputElement);
								setContactName(input.value);
							}}
						/>
						<Input type="text" 
							label={`Purchase Order No. or Reference (Please use your name if PO Numbers are not required)`}
							validate={['Required']}
							defaultValue={customerOrderReference}
							onChange={e => {
								const input = (e.target as HTMLInputElement);
								setCustomerOrderReference(input.value);
							}}
						/>
					</CheckoutSection>
				</li>
			</ol>

			{/* payment step (if necessary) also has its own submit button */}
			{paymentRequired && paymentGateways.ownFormEnabled === false && paymentGateways.hostedPageEnabled === false &&
				<ol className="payment-checkout__steps payment-checkout__steps--continued">
					<li>
						<CheckoutSection title={`Payment`} enabled={true}>
							<ExternalPayment formRef={ssFormRef} disabled={currentStep < CheckoutStep.TermsConditions || !acceptedTerms || !acceptedPrivacy ? true : undefined}/>
						</CheckoutSection>
					</li>
				</ol>
			}

			<Form className="payment-checkout__footer"
				action={user ? shoppingCartApi.checkout : shoppingCartApi.checkoutGuestCart}
				formRef={ssFormRef}				
				failedMessage={`Unable to purchase`}
				loadingMessage={`Purchasing..`}
				onValues={vals => {
					return new Promise((success, reject) => {					

						if (estimates && !deliveryOption) {
							// Required
							reject({message: `Delivery option is required`, type: `field/required`} as PublicError);
							return;
						}

						if (!contactName || contactName.trim().length == 0)
						{
							// Required
							reject({message: `Contact Name is required`, type: `field/required`} as PublicError);
							return;
						}

						if (!customerOrderReference || customerOrderReference.trim().length == 0)
						{
							// Required
							reject({message: `Purchase Order No. or Reference is required`, type: `field/required`} as PublicError);
							return;
						}

						var cartRef = getCartId!();
						vals.nonce = shoppingCart?.revision;
						vals.shoppingCartId = cartRef.id;
						vals.anonymousCartKey = cartRef.anonKey;
						vals.deliveryAddressKey = deliveryAddress?.anonKey;
						vals.billingAddressKey = sameAsDelivery ? deliveryAddress?.anonKey : billingAddress?.anonKey ;
						vals.deliveryOptionKey = deliveryOption!.anonKey;
						vals.deliveryInformation = deliveryInformation; 
						vals.customerOrderReference = customerOrderReference;       
						vals.contactName = contactName;
						
						success(vals);
					});
				}}
				onSuccess={(info : PurchaseAndAction) => {
					if (info?.action) {
						// Go to it now:
						window.location.href = info.action;
					} else {
						var status = info?.purchase?.status || 0;

						if (status == 300) {
							setChallengeMetaData(info);
						}
						else if (status == 201 || status == 202) {
								// Clear cart:
								emptyCart!();
								if (info?.metaData.token && info?.metaData.token.length > 0) {
									setPage(`/cart/purchases/token/${info?.metaData.token}?status=success`);
								} else {
									setPage(`/cart/complete?status=success`);
								}
						} else if (status < 300) {
							if (info?.metaData.token && info?.metaData.token.length > 0) {
								setPage(`/cart/purchases/token/${info?.metaData.token}?status=pending`);
							} else {
								setPage(`/cart/complete?status=pending`);
							}
						} else {
							if (info?.metaData.token && info?.metaData.token.length > 0) {
								setPage(`/cart/purchases/token/${info?.metaData.token}`);
						} else {
								setPage('/cart/complete?status=failed');
							}
						}
					}
				}}
			>

				{(!paymentRequired || (paymentRequired && paymentGateways.ownFormEnabled === true))  && 
					<ol className="payment-checkout__steps payment-checkout__steps--continued">
						<li className={currentStep < CheckoutStep.PaymentMethod && currentStep != CheckoutStep.TransactionApproval ? "payment-checkout__step--disabled" : ""}>
							{/* payment method */}
							<CheckoutSection title={`Payment`} enabled={currentStep >= CheckoutStep.PaymentMethod} >
								{!paymentRequired ? <>
									{`Buy now pay later: This order will be billed to your account.`}
								</> :
									<Input type='payment' name='paymentMethod' label='Payment method' validate={['Required']} />
								}

							</CheckoutSection>
						</li>
					</ol>
				}

				{challengeMetaData && challengeMetaData.metaData && challengeMetaData.metaData.challengeRequest && 
					<Modal className="payment-checkout__3ds" noHeader noFooter isExtraLarge visible>
						{/* approval currentStep */}
						<Challenge width={`100%`} height={`100%`} metaData={challengeMetaData.metaData} />
						<ChallengeChecker metaData={challengeMetaData.metaData} 
							onChange={(result:PurchaseStatus) => {
								if (result.status >= 200 && result.status < 300) {
									if (result.status != 250) {
										// Clear cart:
										emptyCart!();

										if (challengeMetaData?.metaData?.token && challengeMetaData?.metaData?.token.length > 0) {
											setPage(`/cart/purchases/token/${challengeMetaData?.metaData.token}?status=success`);
										} else {
											setPage(`/cart/purchases/${challengeMetaData?.purchase?.id}?status=success`);
										}
									}
								} else if (result.status < 300) {
									setPage(`/cart/complete?status=pending&ref=${challengeMetaData?.purchase?.reference}`);
								} else {
									if (challengeMetaData?.metaData?.token && challengeMetaData?.metaData?.token.length > 0) {
										setPage(`/cart/purchases/token/${challengeMetaData?.metaData.token}?status=failed`);
									} else {
										setPage(`/cart/purchases/${challengeMetaData?.purchase?.id}?status=failed`);
									}
								}
							}}							
						/> 
					</Modal>
				}

				{currentStep >= CheckoutStep.TermsConditions && <>
					{user ?
						<small className="payment-checkout__note">
							{`Please note, by placing your order you agree to both the `}
							<Link href="/terms-and-conditions" external>
								{`terms and conditions`}
							</Link>
							{` and `}
							<Link href="/privacy-policy" external>
								{`privacy policy`}
							</Link>.
						</small>
						: <>
							<Input noWrapper type="checkbox" className="payment-checkout__terms"
								checked={acceptedTerms ? true : undefined}
								validate={['Required']}
								onChange={e => setAcceptedTerms(e.target.checked)} label={
									<>
										{`I confirm that I have read and agree to the `}
										<Link href="/terms-and-conditions" external>
											{`terms and conditions`}
										</Link>
									</>
								}
							/>

							<Input noWrapper type="checkbox" className="payment-checkout__privacy"
								checked={acceptedPrivacy ? true : undefined}
								validate={['Required']}
								onChange={e => setAcceptedPrivacy(e.target.checked)} label={
									<>
										{`I confirm that I have read the `}
										<Link href="/privacy-policy" external>
											{`privacy policy`}
										</Link>.
									</>
								}
							/>
						</>

					}
				</>}			

				{/* merchant hosted payment page */}
				{paymentRequired && paymentGateways.hostedPageEnabled &&
					<ExternalPayment disabled={currentStep < CheckoutStep.TermsConditions || !acceptedTerms || !acceptedPrivacy ? true : undefined} />
				}

				{/* hide the core submit if using card and merchants form */}
				{(!paymentRequired || (paymentRequired && paymentGateways.ownFormEnabled === true))  && 
					<div className="payment-checkout__footer">
						<Button type="submit" disabled={currentStep < CheckoutStep.TermsConditions || !acceptedTerms || !acceptedPrivacy ? true : undefined}>
							<i className="fal fa-fw fa-credit-card" />
							<span>
								{`Confirm Purchase`}
							</span>
						</Button>
					</div>
				}

			</Form>

		</div>
	</>;
}

export default Checkout;