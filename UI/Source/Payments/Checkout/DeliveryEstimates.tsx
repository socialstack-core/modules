import deliveryOptionApi, { DeliveryOption } from 'Api/DeliveryOption';
import { ApiList } from 'UI/Functions/WebRequest';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { addMinutes } from "UI/Functions/DateTools";
import Alert from 'UI/Alert';
import Input from 'UI/Input';

/**
 * Props for the Checkout component.
 */
interface DeliveryEstimatesProps {
	estimates: ApiList<DeliveryOption>;

	value?: DeliveryOption;

	setValue: (val: DeliveryOption) => void;
}

const DeliveryEstimates: React.FC<DeliveryEstimatesProps> = (props) => {
	const { value, setValue, estimates } = props;

	if (!estimates?.results?.length) {
		return <Alert variant="warning">
			{`Unfortunately we are unable to deliver this order. Please get in touch for more details.`}
		</Alert>;	
	}

	return <div className="payment-checkout__delivery-estimates">
		{estimates.results.map(estimate => {
			// Unpack the estimate:
			var estimateDetail = JSON.parse(estimate.informationJson || '');

			// A singular delivery option can contain 1 or more
			// (usually 1) delivery on different days.

			// For example, you can choose to get things as soon as possible with 5/6 items showing up tomorrow
			// and 1/6 the day after. Or you can choose to get all 6 the day after.

			return <Input type="radio" name={"delivery_estimate"} value={estimate.id.toString()} checked={value?.id == estimate.id} noWrapper
				onChange={() => setValue(estimate)}
				label={estimateDetail.deliveries.map(deliveryInfo => {
					// DeliveryInfo C# type, in DeliveryEstimate.cs
					let dateDescription: String;

					// If there are >1 deliveries, then deliveryInfo.Products is
					// set indicating what is present in *this* proposed delivery.

					if (!deliveryInfo.slotStartUtc) {
						// A date isn't known for this service.
						// It happens on JIT style delivery where the product is ordered from an upstream usually foreign supplier.
						// "We'll let you know when a date is confirmed" etc.
						dateDescription = `Date to be confirmed`;
					} else {
						var date = new Date(deliveryInfo.slotStartUtc);

						if (deliveryInfo.timeWindowLength) {
							// It's a time slot range. Date is the start of the slot.
							var slotEnd = addMinutes(date, deliveryInfo.timeWindowLength);

							let from = new Intl.DateTimeFormat('en-GB', {
								year: 'numeric',
								month: 'long',
								day: 'numeric',
								hour: '2-digit',
								minute: '2-digit'
							}).format(date);

							let to = new Intl.DateTimeFormat('en-GB', {
								year: 'numeric',
								month: 'long',
								day: 'numeric',
								hour: '2-digit',
								minute: '2-digit'
							}).format(slotEnd);

							// The time slot is between date and slotEnd.
							dateDescription = `${from} - ${to}`;
						} else {
							// Only use the day component. Time must be ignored.
							dateDescription = `${new Intl.DateTimeFormat('en-GB', {
								year: 'numeric',
								month: 'long',
								day: 'numeric'
							}).format(date)}`;
						}
					}

					return <div className="payment-checkout__delivery-estimate">
						{/* temporarily hidden
						<h3 className="payment-checkout__delivery-estimate__date">
							{dateDescription}
						</h3>
						*/}
						<strong className="payment-checkout__delivery-estimate__name">
							{deliveryInfo.deliveryName}
						</strong>
						<p className="payment-checkout__delivery-estimate__notes">
							{deliveryInfo.deliveryNotes}
						</p>
						<p className="payment-checkout__delivery-estimate__price">
							{formatCurrency(estimateDetail.price, { currencyCode: estimateDetail.currency })}
						</p>
					</div>
				})} />;

		})}
	</div>;

};

export default DeliveryEstimates;