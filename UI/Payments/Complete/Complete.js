import Alert from 'UI/Alert';
import parseQueryString from 'UI/Functions/ParseQueryString';
import { useRouter } from 'UI/Session';

export default function Complete(props) {
	const { setPage } = useRouter();
	var queryObj = parseQueryString();

	switch (queryObj.status) {
		case 'success':
			return <div className="payment-complete">
				<Alert variant='success'>
					<h2 className="stripe-complete-intent__title">
						{`Subscription Complete`}
					</h2>
					<p>
						{`Thank you for your purchase!`}&nbsp;&nbsp;
						<a href='/my-subscriptions' className="alert-link">
							{`View your subscriptions`}
						</a>;
					</p>
				</Alert>
			</div>;

		case 'pending':
			return <div className="payment-complete">
				<Alert variant='info'>
					<h2 className="stripe-complete-intent__title">
						{`Subscription Pending`}
					</h2>
					<p>
						{`It looks like your details are still processing. We'll update you when processing is complete.`}
					</p>
				</Alert>
			</div>;

		case 'failed':
			return <div className="payment-complete">
				<Alert variant='danger'>
					<h2 className="stripe-complete-intent__title">
						{`Subscription Failed`}
					</h2>
					<p>
						{`Failed to process payment details. Please`} <a href='/checkout' className="alert-link">{`click here`}</a> {`to try another payment method.`}
					</p>
				</Alert>
			</div>;

		default:
			// invalid
			setPage('/');
			break;
    }
}

Complete.propTypes = {
};

Complete.defaultProps = {
}

Complete.icon='check';
