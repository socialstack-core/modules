
interface CheckoutSectionProps {
	title?: string;
	enabled?: boolean;
}

const CheckoutSection: React.FC<React.PropsWithChildren<CheckoutSectionProps>> = (props) => {
	const { title, children, enabled } = props;

	return <>
		<section className="payment-checkout__section">
			{title?.length > 0 && <>
				<h2 className="payment-checkout__subtitle">
					{title}
				</h2>
			</>}
			<div className="payment-checkout__section-internal">
				{enabled ? children : `Please complete the steps above first.`}
			</div>
		</section>
	</>;
}

export default CheckoutSection;