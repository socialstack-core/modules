import { formatCurrency } from "UI/Functions/CurrencyTools";
import Alert from 'UI/Alert';
import { recurrenceText } from 'UI/Functions/Payments';
import BasketItem from 'UI/Product/BasketItem';
import { ShoppingCart } from 'Api/ShoppingCart';

/**
 * Props for the ProductTable component.
 */
interface ProductTableProps {
	/**
	 * basket contents
	 */
	shoppingCart: ShoppingCart,

	/** 
	 * set true if exclusive of VAT
	 */
	lessTax?: boolean,

	/** 
	 * set true if contents should not be editable
	 */
	readOnly?: boolean,
}

/**
 * The ProductTable React component.
 * @param props React props.
 */
const ProductTable: React.FC<ProductTableProps> = (props) => {
	var { shoppingCart, readOnly, lessTax } = props;
	var pricedCart = shoppingCart?.cartContents;

	if (!pricedCart || !pricedCart.contents.length) {
		return <Alert type="info">
			{readOnly ? <>
				{`This purchase is empty`}
			</> : <>
				{`Your shopping cart is empty.`}
			</>}

		</Alert>;
	}

	var itemSet = pricedCart.contents;
	var currencyCode = pricedCart.currencyCode;
	//var hasAtLeastOneSubscription = pricedCart.hasSubscriptionProducts;

	return <>
		<ul className="shopping-cart__table">

			{itemSet.map(lineItem => {
				var product = lineItem.product;

				// subscription
				if (product.billingFrequency) {
					// TODO - either extend UI/Product/Signpost to account for subscriptions,
					//        or introduce a dedicated version
					return;

					var formattedCost = formatCurrency(lessTax ? lineItem.totalLessTax : lineItem.total, { currencyCode });

					if (product.billingFrequency) {
						formattedCost += ' ' + recurrenceText(product.billingFrequency);
					}

					{/*
					return <li>
						<td>
							{product.name} <span className="footnote-asterisk" title={`Subscription`}></span>
						</td>
						<td className="qty-column">
							{qty}
						</td>
						<td className="currency-column">
							{formattedCost}
						</td>
						{!readOnly && <td className="actions-column">
							<button type="button" className="btn btn-small btn-outline-danger" title={`Remove`}
								onClick={() => {
									addToCart(product.id, 0)
								}}>
								<Icon type='fa-trash' />
							</button>
						</td>}
					</li>;
					*/}
				}

				// standard quantity of product
				return <li>
					<BasketItem content={product}
						disableLink={false} // prevent clicking to view product details - potentially allow this, but open in a new window?
						priceOverride={{
							currencyCode: currencyCode,
							amount: lessTax ? lineItem.totalLessTax : lineItem.total
						}}
						qtyOverride={lineItem.quantity}
						showRemove={!readOnly}
						readOnly={readOnly} />
				</li>;
			})}
		</ul>
	</>;
}

export default ProductTable;