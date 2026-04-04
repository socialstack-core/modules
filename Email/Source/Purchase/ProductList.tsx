import { Purchase } from 'Api/Purchase';
import ProductPrice from 'UI/Product/Price';
import Image from 'UI/Image';
import { emailStyles } from './EmailStyles';

/**
 * Props for the Product List component (purchases).
 * @icon fal fa-shopping-cart
 * @description Displays a table of purchased products with prices and quantities.
 */
interface ProductListProps {
	purchase?: Purchase;
	lessTax?: boolean;
}

/**
 * The ProductList React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const ProductList: React.FC<ProductListProps> = (props) => {
	let { purchase, lessTax = false } = props;
	
	if (!purchase) {
		return null;
	}
	
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
					<th style={emailStyles.productTableHeaderRight}>
						Price Per Item
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
				{purchase.productQuantities?.map((pq, index) => { 
					if (!pq) {
						return null;
					}

					const product = pq?.product;
					
					return (
						<tr key={index}>
							<td style={emailStyles.productTableCell}>
								<Image plain={true} size={64} fileRef={product.featureRef} />
							</td>
							<td style={emailStyles.productTableCell}>
								{product?.sku}
							</td>
							<td style={emailStyles.productTableCell}>
								{product?.name}
							</td>
							<td style={emailStyles.productTableCellRight}>
								<ProductPrice product={product} currentPriceOnly={true} compact={true}
									override={{
										currencyCode: pq.orderedCurrencyCode!,
										/* The per-item price is calculated here because there may be percentage coupons etc that were applied
										and caused the effective ordered per-item price to go down. */
										amount: pq.quantity ? Math.floor((lessTax ? pq.orderedTotalLessTax : pq.orderedTotal) / pq.quantity) as int : 0 as int
									}} />
							</td>
							<td style={emailStyles.productTableCell}>
								{pq.quantity}
							</td>
							<td style={emailStyles.productTableCellRight}>
								<ProductPrice product={product} currentPriceOnly={true} compact
									override={{
										currencyCode: pq.orderedCurrencyCode!,
										amount: lessTax ? pq.orderedTotalLessTax : pq.orderedTotal
									}} showSellUnit={false}/>
							</td>
						</tr>
					);
				})}
			</tbody>
		</table>
	);
}

export default ProductList;