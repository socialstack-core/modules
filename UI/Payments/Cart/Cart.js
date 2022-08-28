import Wrapper from 'UI/BillingWrapper';
import Modal from 'UI/Modal';
import Alert from 'UI/Alert';
import Loop from 'UI/Loop';
import { useSession, useRouter } from 'UI/Session';
import { useCart } from '../CartSession';
import { useToast } from 'UI/Functions/Toast';
import { useState } from 'react';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { getSteps, getStepIndex } from 'UI/Checkout/Steps.js';

const STRATEGY_PAYG = 0;
const STRATEGY_BULK = 1;

export default function Cart(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	const { pop } = useToast();
	var { addToCart, emptyCart, shoppingCart, getCartQuantity, cartIsEmpty } = useCart();

	var [showEmptyCartPrompt, setShowEmptyCartPrompt] = useState(null);

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
		<Wrapper className="shopping-cart" activeStep={getStepIndex(getSteps().SUBSCRIPTION_TYPE)}>
			<h2 className="shopping-cart__title">
				{`Shopping Cart`}
			</h2>

			<table className="table table-striped shopping-cart__table">
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
						<th className="actions-column">
							&nbsp;
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
						<td colspan="4">
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
							var hasSubscriptions = false;
							var hasPAYG = false;

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
											var showPaygNote = product.priceStrategy == STRATEGY_PAYG;

											switch (product.priceStrategy) {
												case STRATEGY_BULK:
													cost = product.price.amount * product.minQuantity;
													formattedCost = formatCurrency(cost, session.locale) + ` pm`;
													formattedSeats = new Intl.NumberFormat(session.locale.code).format(product.minQuantity);
													break;

												case STRATEGY_PAYG:
													cost = getTierPrice(product, cartInfo.quantity);
													formattedCost = formatCurrency(cost, session.locale) + `/seat pm`;
													formattedSeats = <>&mdash;</>;
													hasPAYG = true;
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
												hasSubscriptions = true;

												return <tr>
													<td>
														{product.name} <span className="footnote-asterisk" title={`Subscription`}></span> {showPaygNote && <span className="footnote-dagger" title={`PAYG`}></span>}
													</td>
													<td className="qty-column">
														{formattedSeats}
													</td>
													<td className="currency-column">
														{formattedCost}
													</td>
													<td className="actions-column">
														<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
															onClick={() => {
																addToCart({
																	product: product.id,
																	quantity: -getCartQuantity(product.id),
																	isSubscribing: true})
                                                            }}>
															<i className="fal fa-fw fa-trash"></i>
														</button>
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
												<td className="actions-column">
													<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
														onClick={() => {
															addToCart({
																product: product.id,
																quantity: -getCartQuantity(product.id),
																isSubscribing: true
															})
														}}>
														<i className="fal fa-fw fa-trash"></i>
													</button>
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
										<td>
											&nbsp;
										</td>
									</tr>
								</tfoot>
								<caption>
									{hasSubscriptions && <small>
										<span className="footnote-asterisk"></span> {`Your payment information will be securely stored in order to process future subscription payments`}
									</small>}
									{hasPAYG && <small>
										<span className="footnote-dagger"></span> {`An initial authorising payment covering the cost of a single seat will be taken for Pay-As-You-Go subscriptions`}
									</small>}
								</caption>
							</>;
						}
					}
				</Loop>
			</table>

			{!cartIsEmpty() && <>
				<div className="shopping-cart__footer">
					<button type="button" className="btn btn-outline-danger" onClick={() => setShowEmptyCartPrompt(true)}>
						<i className="fal fa-fw fa-trash" />
						{`Empty Cart`}
					</button>

					<button type="button" className="btn btn-primary" onClick={() => setPage('/checkout')}>
						<i className="fal fa-fw fa-credit-card" />
						{`Checkout`}
					</button>
				</div>
			</>}
		</Wrapper>
		{
			showEmptyCartPrompt && <>
				<Modal visible isSmall className="empty-cart-modal" title={`Empty Cart`} onClose={() => setShowEmptyCartPrompt(false)}>
					<p>{`This will remove all selected products from your shopping cart.`}</p>
					<p>{`Are you sure you wish to do this?`}</p>
					<footer>
						<button type="button" className="btn btn-outline-danger" onClick={() => setShowEmptyCartPrompt(false)}>
							{`Cancel`}
						</button>
						<button type="button" className="btn btn-danger" onClick={() => {
							emptyCart();
							setShowEmptyCartPrompt(false);
						}}>
							{`Empty`}
						</button>
					</footer>
				</Modal>
			</>
		}
	</>
}

Cart.propTypes = {
};

Cart.defaultProps = {
}

Cart.icon='shopping-cart';
