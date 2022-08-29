import { formatCurrency } from "UI/Functions/CurrencyTools";
import Loop from 'UI/Loop';
import { useSession } from 'UI/Session';
import Alert from 'UI/Alert';

const STRATEGY_STD = 0;
const STRATEGY_STEP1 = 1;

export default function ProductTable(props){
	
	var {shoppingCart, addToCart} = props;
	var {session} = useSession();
	
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
	
	return <table className="table shopping-cart__table">
		<thead>
			<tr>
				<th>
					{`Product`}
				</th>
				<th className="qty-column">
					{`Quantity`}
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
					
					return <tbody>
						{
							shoppingCart.items.map(cartInfo => {
								var product = allProducts.find(prod => prod.id == cartInfo.product);

								if (!product) {
									// product withdrawn in some way
									return null;
								}

								var cost;
								var showPaygNote = product.priceStrategy == STRATEGY_STD;

								switch (product.priceStrategy) {
									case STRATEGY_STEP1:
										cost = product.price.amount * product.minQuantity;
										break;

									case STRATEGY_STD:
										cost = getTierPrice(product, cartInfo.quantity);
										break;

									default:
										cost = 0;
										break;
								}

								cartTotal += cost;
								
								var formattedCost = formatCurrency(cost, session.locale);
								var formattedUnits = new Intl.NumberFormat(session.locale.code).format(product.minQuantity);
								
								// subscription
								if (cartInfo.isSubscribing) {
									hasSubscriptions = true;

									return <tr>
										<td>
											{product.name} <span className="footnote-asterisk" title={`Subscription`}></span> {showPaygNote && <span className="footnote-dagger" title={`PAYG`}></span>}
										</td>
										<td className="qty-column">
											{formattedUnits}
										</td>
										<td className="currency-column">
											{formattedCost}
										</td>
										<td className="actions-column">
											<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
												onClick={() => {
													addToCart({
														product: product.id,
														quantity: -cartInfo.quantity,
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
										{formatCurrency(product.price.amount, session.locale)}
									</td>
									<td className="actions-column">
										<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
											onClick={() => {
												addToCart({
													product: product.id,
													quantity: -cartInfo.quantity,
													isSubscribing: true
												})
											}}>
											<i className="fal fa-fw fa-trash"></i>
										</button>
									</td>
								</tr>;
							})
						}
						<tr>
							<td>
								{hasSubscriptions && <small>
									<span className="footnote-asterisk"></span> {`Your payment information will be securely stored in order to process future subscription payments. The total stated will also be charged today.`}
								</small>}
							</td>
							<td className="qty-column">
								{`TOTAL`}:
							</td>
							<td className="currency-column">
								<b>{formatCurrency(cartTotal, session.locale) + ` pm`}</b>
							</td>
							<td>
								&nbsp;
							</td>
						</tr>
					</tbody>;
				}
			}
		</Loop>
	</table>;
}