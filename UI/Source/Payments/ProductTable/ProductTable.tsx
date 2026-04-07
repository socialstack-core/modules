import { formatCurrency } from "UI/Functions/CurrencyTools";
import Alert from 'UI/Alert';
import { recurrenceText } from 'UI/Functions/Payments';
import BasketItem from 'UI/Product/BasketItem';
import { ShoppingCart } from 'Api/ShoppingCart';
import CartTotal from 'UI/Payments/CartTotal';
import ProductPrice from "UI/Product/Price";
import { PriceCurrency } from "Api/Content";
import Quantity from "UI/Product/Quantity";

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

	/**
	 * set true to render as a basic table
	 */
	tableFormat?: boolean
}

/**
 * The ProductTable React component.
 * @param props React props.
 */
const ProductTable: React.FC<ProductTableProps> = (props) => {
	var { shoppingCart, readOnly, lessTax, tableFormat } = props;
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

	if (tableFormat) {
		return <>
			<div className="shopping-cart__plain-table">
				<table className="table ui-table ui-table--xs">
					<thead>
						<tr>
							{/*
						<th>
							<span className="sr-only">{`Product image`}</span>
						</th>
						*/}
							<th>{`Code`}</th>
							<th>{`Product Name`}</th>
							<th>{`Category`}</th>
							{/*
							<th className="ui-table__col--currency ui-table__col--right">{`Price per item`}</th>
							*/}
							<th className="ui-table__col--qty">{`Quantity`}</th>
							<th className="ui-table__col--currency ui-table__col--right">{`Total Price`}</th>
						</tr>
					</thead>
					<tbody>
						{itemSet.map(lineItem => {
							var product = lineItem.product;

							// subscription
							if (product.billingFrequency) {
								return;
							}

							// standard quantity of product
							//const unitAmount = ;
							const totalAmount = lessTax ? lineItem.totalLessTax : lineItem.total;

							return <>
								<tr key={String(lineItem.id)}>
									<td>{product?.sku}</td>
									<td>{product?.name}</td>
									<td>{lineItem.product?.primaryCategory?.name ?? "-"}</td>
									{/*
									<td className="ui-table__col--currency ui-table__col--right">
										<ProductPrice
											product={product}
											currentPriceOnly={true}
											compact={true}
											override={{
												currencyCode: currencyCode ?? "GBP",
												amount: unitAmount
											}}
										/>
									</td>
									*/}
									<td className="ui-table__col--qty">
										<Quantity
											compact
											product={product}
											qtyOverride={lineItem.quantity}
											readOnly={true}
										/>
									</td>
									<td className="ui-table__col--currency ui-table__col--right">
										<ProductPrice
											product={product}
											currentPriceOnly={true}
											compact={true}
											override={{
												currencyCode: currencyCode ?? "GBP",
												amount: totalAmount
											}}
										/>
									</td>
								</tr>
							</>;
						})}
					</tbody>
				</table>
			</div>
			<CartTotal shoppingCart={shoppingCart} lessTax={lessTax} hideCTAs={true} />
		</>;
	}

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