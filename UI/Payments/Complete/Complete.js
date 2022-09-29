import Alert from 'UI/Alert';
import parseQueryString from 'UI/Functions/ParseQueryString';
import { useRouter, useSession } from 'UI/Session';

export default function Complete(props) {
	const { setPage } = useRouter();
	var { sessionReload } = useSession();
	var queryObj = parseQueryString();
	
	React.useEffect(() => {
		
		if(!props.noSessionUpdate){
			// Force a session refresh. This is because a payment may have been for a subscription which affects the session state.
			sessionReload();
		}
		
	}, []);
	
	switch (queryObj.status) {
		case 'success':
			return <div className="payment-complete">
				<Alert variant='success'>
					<h2 className="stripe-complete-intent__title">
						{`Purchase Complete`}
					</h2>
					<p>
						{`Thank you for your purchase!`}&nbsp;&nbsp;
						<a href='/my-subscriptions' className="alert-link">
							{`View your subscriptions`}
						</a>
					</p>
				</Alert>
			</div>;

		case 'pending':
			return <div className="payment-complete">
				<Alert variant='info'>
					<h2 className="stripe-complete-intent__title">
						{`Purchase Pending`}
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
						{`Purchase Failed`}
					</h2>
					<p>
						{`Failed to process payment details. Please`} <a href='/checkout' className="alert-link">{`click here`}</a> {`to try another payment method.`}
					</p>
				</Alert>
			</div>;

		case 'card-update.success':
			return <div className="payment-complete">
				<Alert variant='success'>
					<h2 className="stripe-complete-intent__title">
						{`Card Update Complete`}
					</h2>
					<p>
						<a href='/my-subscriptions' className="alert-link">
							{`View your subscriptions`}
						</a>
					</p>
				</Alert>
			</div>;
		case 'card-update.failed':
			return <div className="payment-complete">
				<Alert variant='danger'>
					<h2 className="stripe-complete-intent__title">
						{`Card Update Failed`}
					</h2>
					<p>
						{`Failed to process payment details. Please try another payment method.`}
					</p>
				</Alert>
			</div>;

		default:
			// invalid
			return `Unknown payment status`;
		break;
    }
}

Complete.propTypes = {
};

Complete.defaultProps = {
}

Complete.icon='check';
