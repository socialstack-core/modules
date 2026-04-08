import { useEffect ,useState } from "react";
import { PaymentMethod } from 'Api/PaymentMethod';
import Alert from 'UI/Alert';

/**
 */
interface ExternalPaymentProps
{
	formRef: React.RefObject<HTMLFormElement>;
    disabled?: boolean; 
}

/**
 * The ExternalPayment React component.
 * @param props React props.
 */
const ExternalPayment: React.FC<ExternalPaymentProps> = (props) => {
	const { 
		disabled,
        formRef
	} = props;
	
    const [error, setError] = useState<PublicError | undefined>();
	const [paymentMethod, setPaymentMethod] = useState<PaymentMethod| undefined>();

	var paymentGateways = global.paymentGateways = global.paymentGateways || {};

	useEffect(() => {

		var formload = paymentGateways.hostedPageEnabled ? paymentGateways.onHostedPage() : paymentGateways.onGetMerchantKey();
		
		formload.then(data => {
			setPaymentMethod(data);
		}).catch(err => {
            setError(err);
		});;

	},[])

	var Component = paymentMethod?.component;

	return <>
		{paymentMethod && 
			<Component paymentMethod={paymentMethod} formRef={formRef} disabled={disabled}/>
		}
		
		{error && 
			<Alert variant='danger'>
				{error.message || `An issue occurred when attempting to checkout this order. Please try again later - if this keeps happening, please let us know.`}
			</Alert>
		}
	</>;
		
};

export default ExternalPayment;