import { Purchase } from 'Api/Purchase';
import { PurchaseToken } from 'Api/PurchaseToken';
import ProductList from './ProductList';
import Summary from './Summary';
import Addresses from './Addresses';
import { emailStyles } from './EmailStyles';
import PaymentSummary from './PaymentSummary';

/**
 * Props for the View component.
 * @icon fal fa-receipt
 * @description Displays a complete order with addresses, products, and summary.
 */
interface ViewProps {
	purchaseToken?: PurchaseToken;
	lessTax?: boolean;
	}

/**
 * The View React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const View: React.FC<ViewProps> = (props) => {
	let { purchaseToken, lessTax = false } = props;

	if (!purchaseToken || !purchaseToken.purchase) {
		return null;
	}

	let purchase = purchaseToken.purchase;

	return (
		<div style={emailStyles.viewContainer}>
			<h1 style={emailStyles.pageTitle}>
				{`Order #${purchase.reference}`}
			</h1>

			{/* show the reason for payment failure (if applicable)*/}
			<PaymentSummary purchase={purchase} />            

			<div style={emailStyles.section}>
				{/* guest purchase addresses */}	
				<Addresses billingAddress={purchase.billingAddress} deliveryAddress={purchase.deliveryAddress} />

				{/* item list */}
				<ProductList purchase={purchase} lessTax={lessTax} />
			</div>

			<div>
				<Summary purchase={purchase} lessTax={lessTax} />
			</div>
		</div>
	);
}

export default View;