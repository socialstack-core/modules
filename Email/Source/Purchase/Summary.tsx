import { Purchase } from 'Api/Purchase';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { emailStyles } from './EmailStyles';

/**
 * Props for the Summary component.
 * @icon fal fa-calculator
 * @description Displays a purchase summary with totals, delivery, and VAT.
 */
interface SummaryProps {
	purchase?: Purchase;
	lessTax?: boolean;
}

/**
 * The Summary React component for email generation.
 * Email-friendly version with shared inline styles.
 * @param props React props.
 */
const Summary: React.FC<SummaryProps> = (props) => {
	let { purchase, lessTax } = props;

	if (!purchase) {
		return null;
	}

	const { totalCost, totalCostLessTax,
		deliveryCostLessTax, deliveryCost, productsCostLessTax, productsCost,
		productQuantities
	} = purchase;

	let currencyCode = purchase.currencyCode || undefined;

	const itemCount = productQuantities?.length || 0;

	let subTotal = 0;
	if (productQuantities && itemCount > 0)
	{
		for (const pq of productQuantities) { 
			if (!pq) {
				continue;
			}

			if(lessTax){
				subTotal += pq.orderedTotalLessTax;
			}else{
				subTotal += pq.orderedTotal;
			}
		}
	}

	return (
		<table style={emailStyles.table} cellPadding="0" cellSpacing="0" role="presentation">
			<tbody>
				{/*
					Be extremely careful with the ordering of VAT values - 
					deliveries themselves are not VAT free for example.
				*/}
				{(lessTax ? subTotal != purchase.productsCostLessTax : subTotal != purchase.productsCost) && (
					<>
						<tr style={emailStyles.row}>
							<td style={emailStyles.cellLeft}>
								{lessTax ? `Subtotal (ex VAT)` : `Subtotal (inc VAT)`}
							</td>
							<td style={emailStyles.cellRight}>
								{formatCurrency(subTotal, { currencyCode })}
							</td>
						</tr>
						<tr style={emailStyles.row}>
							<td style={emailStyles.cellLeft}>
								Promotion Applied
							</td>
							<td style={emailStyles.cellRight}>
								{formatCurrency(lessTax ? productsCostLessTax - subTotal : productsCost - subTotal, { currencyCode })}
							</td>
						</tr>
					</>
				)}
				<tr style={emailStyles.row}>
					<td style={emailStyles.cellLeft}>
						{lessTax ? `Total (ex VAT)` : `Total (inc VAT)`}
					</td>
					<td style={emailStyles.cellRight}>
						{formatCurrency(lessTax ? productsCostLessTax : productsCost, { currencyCode })}
					</td>
				</tr>
				<tr style={emailStyles.row}>
					<td style={emailStyles.cellLeft}>
						{lessTax ? `Delivery (ex VAT)` : `Delivery (inc VAT)`}
					</td>
					<td style={emailStyles.cellRight}>
						{formatCurrency(lessTax ? deliveryCostLessTax : deliveryCost, { currencyCode })}
					</td>
				</tr>
				{lessTax && (
					<tr style={emailStyles.row}>
						<td style={emailStyles.cellLeft}>
							VAT
						</td>
						<td style={emailStyles.cellRight}>
							{formatCurrency(totalCost - totalCostLessTax, { currencyCode })}
						</td>
					</tr>
				)}
				<tr style={emailStyles.grandTotalRow}>
					<td style={emailStyles.cellLeft}>
						Total
					</td>
					<td style={emailStyles.cellRight}>
						{formatCurrency(totalCost, { currencyCode })}
					</td>
				</tr>
				{!lessTax && (
					<tr style={emailStyles.row}>
						<td style={emailStyles.cellLeft}>
							VAT
						</td>
						<td style={emailStyles.cellRight}>
							{formatCurrency(productsCost + deliveryCost - productsCostLessTax - deliveryCostLessTax, { currencyCode })}
						</td>
					</tr>
				)}
			</tbody>
		</table>
	);
}

export default Summary;