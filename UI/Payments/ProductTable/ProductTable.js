import { formatCurrency } from "UI/Functions/CurrencyTools";
import Loop from 'UI/Loop';
import Alert from 'UI/Alert';
import { calculatePrice, recurrenceText } from 'UI/Functions/Payments';

const STRATEGY_STD = 0;
const STRATEGY_STEP1 = 1;
const STRATEGY_STEPALWAYS = 2;

export default function ProductTable(props){
	
	var {shoppingCart, addToCart, readonly} = props;
	
	function renderTotals(cartTotals, options){
		var totals = [];
		var coupon = options && options.coupon;
		
		for(var i=0;i<5;i++){
			if(cartTotals[i]){
				var total = cartTotals[i];
				var totalCost = total;
				
				if (coupon != null) {
					if (coupon.minSpendPrice) {		
						// Are we above it?
						if (totalCost < coupon.minSpendPrice.amount) {
							// No!
							coupon = null;
						}
					}
					
					if (coupon && coupon.discountPercent != 0) {
						var discountedTotal = totalCost * (1 - (coupon.discountPercent / 100));

						if (discountedTotal <= 0) {
							// Becoming free!
							totalCost = 0;
						} else {
							// Round to nearest pence/ cent
							totalCost = Math.ceil(discountedTotal);
						}
					}
					
					if (coupon && coupon.discountAmount) {		
						if (totalCost < discountAmount.amount) {
							// Becoming free!
							totalCost = 0;
						} else {
							// Discount a fixed number of units:
							totalCost -= discountAmount.amount;
						}
					}
				}
				
				var recurTitle = recurrenceText(i);
				
				if(totalCost != total){
					if(i){
						// i is 0 for one off payments.
						// This is any recurring things with a discount, where the discount is applied on the first payment only.
						totals.push(<div>{formatCurrency(totalCost, options)} today, then {
							formatCurrency(total, options)
						} {recurTitle}</div>);
					}else{
						totals.push(<div><small><s>{
							formatCurrency(total, options)
						}</s></small> {formatCurrency(totalCost, options)}</div>);
					}
				}else{
					totals.push(<div>{
						formatCurrency(totalCost, options)
					} {recurTitle}</div>);
				}
			}
		}
		
		return totals;
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
				{!readonly && <th className="actions-column">
					&nbsp;
				</th>}
			</tr>
		</thead>
		<Loop over='product' includes={['price', 'tiers', 'tiers.price']} groupAll raw
			filter={{
				where: {
					id: shoppingCart.items.map(cartInfo => (cartInfo.productId || cartInfo.product))
				}
			}}
			orNone={() => <tbody>
				<td colspan="4">
				<Alert type="info">
						{readonly ? <>
							{`This purchase is empty`}
						</> : <>
							{`Your shopping cart is empty.`}&nbsp;&nbsp;
							<a href="/subscribe" className="alert-link">
								{`Click here`}
							</a> {`to add a product`}
						</>}
						
					</Alert>
				</td>
			</tbody>
			}>
			{
				allProducts => {
					var cartTotalByFrequency = [0,0,0,0,0];
					var hasAtLeastOneSubscription = false;
					var currencyCode = null;
					
					return <tbody>
						{
							shoppingCart.items.map(cartInfo => {
								var product = allProducts.find(prod => prod.id == (cartInfo.productId || cartInfo.product));

								if (!product) {
									// product withdrawn in some way
									return null;
								}
								
								var qty = cartInfo.quantity;
								
								if(qty < product.minQuantity){
									qty = product.minQuantity;
								}
								
								var cost = calculatePrice(product, qty);
								
								cartTotalByFrequency[product.billingFrequency] += cost.amount;
								if(!currencyCode){
									currencyCode = cost.currencyCode;
								}
								
								var formattedCost = formatCurrency(cost.amount, cost);
								
								if(product.billingFrequency){
									formattedCost += recurrenceText(product.billingFrequency);
								}
								
								// subscription
								if (product.billingFrequency) {
									hasAtLeastOneSubscription = true;

									return <tr>
										<td>
											{product.name} <span className="footnote-asterisk" title={`Subscription`}>*</span>
										</td>
										<td className="qty-column">
											{qty}
										</td>
										<td className="currency-column">
											{formattedCost}
										</td>
										{!readonly && <td className="actions-column">
											<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
												onClick={() => {
													addToCart({
														product: product.id,
														quantity: -cartInfo.quantity
													})
												}}>
												<i className="fal fa-fw fa-trash"></i>
											</button>
										</td>}
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
										{formatCurrency(product.price.amount, product.price)}
									</td>
									{!readonly && <td className="actions-column">
										<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
												onClick={() => {
													addToCart({
														product: product.id,
														quantity: -cartInfo.quantity
													})
												}}>
												<i className="fal fa-fw fa-trash"></i>
										</button>
									</td>}
								</tr>;
							})
						}
						<tr>
							<td>
							</td>
							<td className="qty-column">
								{`TOTAL`}:
							</td>
							<td className="currency-column" style={{fontWeight: 'bold'}}>
								{currencyCode ? renderTotals(cartTotalByFrequency, {currencyCode, coupon: props.coupon}) : '-'}
							</td>
							<td>
								&nbsp;
							</td>
						</tr>
						<tr>
							<td colspan='3'>
								{hasAtLeastOneSubscription && <small>
									<span className="footnote-asterisk">*</span> {`Your payment information will be securely stored in order to process future subscription payments. The total stated will also be charged today.`}
								</small>}
							</td>
						</tr>
					</tbody>;
				}
			}
		</Loop>
	</table>;
}