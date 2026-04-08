import {ShoppingCart} from "Api/ShoppingCart";
import ProductPrice from './Price';
import Image from 'UI/Image';
import { emailStyles } from './EmailStyles';

/**
 * Props for the Product List component (cart).
 * @icon fal fa-list
 * @description Displays a table of products in the shopping cart.
 */
interface ProductListProps {
	shoppingCart?: ShoppingCart;
	lessTax?: boolean;
}

/**
 * The ProductList React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const ProductList: React.FC<ProductListProps> = ({lessTax = true, ...props}) => {
	let { shoppingCart } = props;

	if (!shoppingCart) {
		return null;
	}

	var pricedCart = shoppingCart?.cartContents;

	if (!pricedCart || !pricedCart.contents || pricedCart.contents.length == 0) {
		return null;
	}

	var itemSet = pricedCart.contents;
	var currencyCode = pricedCart.currencyCode;

    return (
		<table style={emailStyles.productTable} cellPadding="0" cellSpacing="0" role="presentation">
			<thead>
				<tr>
					<th style={emailStyles.productTableHeader}>
					</th>
					<th style={emailStyles.productTableHeader}>
						Code
					</th>
					<th style={emailStyles.productTableHeader}>
						Product Name
					</th>
					<th style={emailStyles.productTableHeader}>
						Quantity
					</th>
					<th style={emailStyles.productTableHeaderRight}>
						Total Price
					</th>
				</tr>
			</thead>
			<tbody>
				{itemSet.map((lineItem,id) => {
					if (!itemSet) {
						return null;
					}

					const pq = shoppingCart.productQuantities?.find(pq => pq?.id == lineItem.productQuantityId);
					var product = pq?.product;

					const totalAmount = lessTax ? lineItem.totalLessTax : lineItem.total;					
				
					return (
						<tr key={id}>
							<td style={emailStyles.productTableCell}>
								<Image plain={true} size={64} fileRef={product?.featureRef} />
							</td>
							<td style={emailStyles.productTableCell}>
								{product?.sku}
							</td>
							<td style={emailStyles.productTableCell}>
								{product?.name}
							</td>
							<td style={emailStyles.productTableCell}>
								{lineItem.quantity}
							</td>
							<td style={emailStyles.productTableCellRight}>
								<ProductPrice
									product={product}
									lessTax={lessTax}
									override={{
										currencyCode: currencyCode ?? "GBP",
										amount: totalAmount
									}}
									showSellUnit={false}
								/>
							</td>
						</tr>
					);
				})}
			</tbody>
		</table>
	);
}

export default ProductList;