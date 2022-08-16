import Wrapper from '../Wrapper';
import Loop from 'UI/Loop';
import Input from 'UI/Input';
import Form from 'UI/Form';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Modal from 'UI/Modal';
import webRequest from 'UI/Functions/WebRequest';
import { getSteps, getStepIndex } from '../Steps.js';
import { useState, useEffect } from 'react';
import { useConfig, useRouter, useSession } from 'UI/Session';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { useToast } from 'UI/Functions/Toast';
import { useCart } from '../CartSession';
import { isoConvert } from "UI/Functions/DateTools";

const STRATEGY_PAYG = 0;
const STRATEGY_BULK = 1;


export default function Checkout(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	var { shoppingCart, cartIsEmpty, hasSubscriptions, emptyCart } = useCart();
	var [termsAccepted, setTermsAccepted] = useState(false);
	
	function purchase() {
		var paymentMethod;

		// chosen an existing card
		if (paymentSelection != null && !isNaN(paymentSelection)) {
			paymentMethod = paymentSelection;
		}

		// added a card
		if (paymentSelection != null && isNaN(paymentSelection)) {
			paymentMethod = {
				gatewayToken: paymentSelection.payment_method,
				gatewayId: STRIPE_GATEWAY_ID
			};
		}

		checkout({
			paymentMethod: paymentMethod
		}).then((info) => {
			var statusOK = true;
			
			if(info.nextAction){
				// Go to it now:
				window.location = info.nextAction;
			}else{
				if (info && !isNaN(parseInt(info.status, 10))) {
					statusOK = info.status >= 200 && info.status < 300;
				}
				
				setPage(statusOK ? '/complete?status=success' : '/complete?status=pending');
			}
		}).catch((e) => {
			console.log(e);
			setPage('/complete?status=failed');
		});

	}

	function canPurchase() {
		return !cartIsEmpty() && termsAccepted;
	}
	
	function getTierPrice(product, quantity) {

		if (!product.tiers || !product.tiers.length) {
			return product.price.amount;
		}

		var price = product.price.amount;

		product.tiers.forEach(tier => {
			if (quantity >= tier.minQuantity) {
				price = tier.price.amount;
			}
		});

		return price;
	}

	return <>
		<Wrapper className="subscribe-checkout" activeStep={getStepIndex(getSteps().SUBSCRIPTION_TYPE)}>
			<h2 className="subscribe-checkout__title">
				{`Checkout`}
			</h2>
			<Form 
				action='purchase/submit'
				failedMessage={`Unable to purchase`}
				loadingMessage={`Purchasing..`}
				onSuccess={info => {
					console.log(info);
					
					// TODO: *must* handle info.NextAction if it is not null.
					
					
					if(info.status >= 200 && info.status < 300){
						setPage('/complete?status=success');
					}else if(info.status < 300){
						setPage('/complete?status=pending');
					}else{
						setPage('/complete?status=failed');
					}
					
				}}
			>
				<div className="mb-3">

					<table className="table table-striped subscribe-checkout__table">
						<thead>
							<tr>
								<th>
									{`Product`}
								</th>
								<th className="qty-column">
									{`Seats`}
								</th>
								<th className="currency-column">
									{`Cost`}
								</th>
							</tr>
						</thead>
						<Loop over='product' includes={['price', 'tiers', 'tiers.price']} groupAll raw
							filter={{
								where: {
									id: shoppingCart.items.map(cartInfo => cartInfo.product)
								}
							}}
							orNone={() => <tbody>
								<td colspan="3">
									<Alert type="info">
										{`Your shopping cart is empty.`}&nbsp;&nbsp;
										<a href="/subscribe" className="alert-link">
											{`Click here`}
										</a> {`to add a product`}
									</Alert>
								</td>
							</tbody>
							}>
							{
								allProducts => {
									var cartTotal = 0;

									return <>
										<tbody>
											{
												shoppingCart.items.map(cartInfo => {
													var product = allProducts.find(prod => prod.id == cartInfo.product);

													if (!product) {
														// product withdrawn in some way
														return null;
													}

													var cost, formattedCost, formattedSeats;

													switch (product.priceStrategy) {
														case STRATEGY_BULK:
															cost = product.price.amount * product.minQuantity;
															formattedCost = formatCurrency(cost, session.locale, { hideDecimals: true }) + ` pm`;
															formattedSeats = new Intl.NumberFormat(session.locale.code).format(product.minQuantity);
															break;

														case STRATEGY_PAYG:
															cost = getTierPrice(product, cartInfo.quantity);
															formattedCost = formatCurrency(cost, session.locale) + `/seat pm`;
															formattedSeats = new Intl.NumberFormat(session.locale.code).format(cartInfo.quantity);
															break;

														default:
															cost = 0;
															formattedCost = '';
															formattedSeats = <>&mdash;</>;
															break;
													}

													cartTotal += cost;

													// subscription
													if (cartInfo.isSubscribing) {
														return <tr>
															<td>
																{product.name} <span className="footnote-asterisk" title={`Subscription`}></span>
															</td>
															<td className="qty-column">
																{formattedSeats}
															</td>
															<td className="currency-column">
																{formattedCost}
															</td>
														</tr>;
													}

													// standard quantity of product
													return <tr>
														<td>
															{product.name}
														</td>
														<td className="qty-column">
															{cartInfo.quantity}
														</td>
														<td className="currency-column">
															{formatCurrency(product.price.amount, session.locale)} + ` pm`
														</td>
													</tr>;
												})
											}
										</tbody>
										<tfoot>
											<tr>
												<td>
													&nbsp;
												</td>
												<td className="qty-column">
													{`TOTAL`}:
												</td>
												<td className="currency-column">
													{formatCurrency(cartTotal, session.locale) + ` pm`}
												</td>
											</tr>
										</tfoot>
										{hasSubscriptions() && <caption>
											<small>
												<span className="footnote-asterisk"></span> {`Your payment information will be securely stored in order to process future subscription payments`}
											</small>
										</caption>}
									</>;
								}
							}
						</Loop>
					</table>
					
					<input type='hidden' name='items' ref={ir=>{
						if(ir){
							ir.onGetValue=(val, ele)=>{
								if(ele == ir){
									return shoppingCart.items;
								}
							};
						}
					}} />
					
					<Input type='payment' name='paymentMethod' label='Payment method' validate={['Required']} />
					
					{!cartIsEmpty() && <>
						<div class="form-check">
							<input class="form-check-input" type="checkbox" id="termsCheckbox" checked={termsAccepted ? 'checked' : undefined}
								onChange={e => setTermsAccepted(e.target.checked)} />
							<label class="form-check-label" htmlFor="termsCheckbox">
								{`I have read and agree to both the `}
								<a href="/terms-and-conditions" target="_blank" rel="noopener noreferrer">
									{`terms and conditions`}
								</a>
								{` and `}
								<a href="/privacy-policy" target="_blank" rel="noopener noreferrer">
									{`privacy policy`}
								</a>
							</label>
						</div>
					</>}

				</div>
				<div className="subscribe-checkout__footer">
					<button type="submit" className="btn btn-primary"
						disabled={!canPurchase() ? "disabled" : undefined}>
						<i className="fal fa-fw fa-credit-card" />
						{`Confirm Purchase`}
					</button>
				</div>
			</Form>
		</Wrapper>
	</>;
}

Checkout.propTypes = {
};

Checkout.defaultProps = {
}

Checkout.icon='register';
