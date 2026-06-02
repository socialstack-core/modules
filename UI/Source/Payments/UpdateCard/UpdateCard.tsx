import Input from 'UI/Input';
import Form from 'UI/Form';
import Button from 'UI/Button';
import { useRouter } from 'UI/Router';
import subscriptionApi from 'Api/Subscription';

/**
 * @icon fal fa-register
 */
interface UpdateCardProps {
}

/**
 * The UpdateCard React component.
 * @param props React props.
 */
const UpdateCard: React.FC<UpdateCardProps> = ({}) => {
	const { pageState, setPage } = useRouter();
	return <div className="update-card">
		<h2 className="payment-checkout__title">
			{`Update Card`}
		</h2>
		<Form
			action={body => subscriptionApi.updateCard(parseInt(pageState.tokens[0] || '') as int, body)}
			failedMessage={`Unable to update card`}
			loadingMessage={`Updating card details...`}
			onSuccess={info => {

				if (info.status == 200) {
					setPage('/complete?status=card-update.success');
				} else {
					setPage('/complete?status=card-update.failed');
				}
			}}
		>
			<div className="mb-3">
				<Input type='payment' name='paymentMethod' updateMode={true} label={`Payment method`} validate={['Required']} />
			</div>
			<div className="payment-checkout__footer">
				<Button type="submit">
					<i className="fal fa-fw fa-credit-card" />
					{`Confirm Update`}
				</Button>
			</div>
		</Form>
	</div>;
}

export default UpdateCard;
