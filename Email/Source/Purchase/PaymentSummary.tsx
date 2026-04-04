import Alert from 'Email/Alert';
import { Purchase } from 'Api/Purchase';

/**
 * Props for the PaymentSummary component.
 * @icon fal fa-credit-card
 * @description Displays a payment failure alert if applicable.
 */
interface PaymentSummaryProps {
	purchase?: Purchase
}

/**
 * The Payment Summary React component.
 * @param props React props.
 */
const PaymentSummary: React.FC<PaymentSummaryProps> = (props) => {
	let {purchase} = props;

	const rawGatewayResponse = purchase?.gatewayPublicJson ? JSON.parse(purchase?.gatewayPublicJson) : [];
	const gatewaySegments = Array.isArray(rawGatewayResponse) ? rawGatewayResponse : [String(rawGatewayResponse)];

	if ((purchase && purchase.status < 400) || gatewaySegments.length === 0) {
		return(null);
	}

	return (
		<Alert variant="danger">
			<h5>{`Payment failed`}</h5>
			<ul style={{ marginTop: 0, marginBottom: 0 }}>
			{gatewaySegments.map((segment, index) => (
				<li key={index}>{segment}</li>
			))}
			</ul>
			<h6>{`Please contact us for assistance with your payment to help you complete your order.`}</h6>
		</Alert>
	);
}


export default PaymentSummary;