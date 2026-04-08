import { useEffect , useState} from "react";
import { useRouter } from 'UI/Router';
import purchaseApi, { Purchase } from 'Api/Purchase';
import Complete from 'UI/Payments/Complete';
import Loading from 'UI/Loading';

/**
 * Props for the HostedStatus component.
 */
interface HostedStatusProps {
	token: string;
}

/**
 * Wrapper component to show the result the hosted payment response which is only available via token.
 */
const HostedStatus: React.FC<HostedStatusProps> = (props) => {

	const { token } = props;    
    const [purchase, setPurchase] = useState<Purchase | undefined>();

	const { pageState } = useRouter();
	const { query } = pageState;

    useEffect(() => {

        var hostedPageResponse = {
            "token": token,
            "status": query.get('state') || '',
            "registrationId": query.get('registrationId') || '',
            "transactionId": query.get('transactionId') || '',
            "reference": query.get('vendorTxCode') || ''
        }

		purchaseApi.validateHostedPageResponse(hostedPageResponse,
			[
				purchaseApi.includes.creatorUser,
				purchaseApi.includes.billingAddress,
				purchaseApi.includes.deliveryAddress,
				purchaseApi.includes.productquantities.orderStatus,
				purchaseApi.includes.productquantities.product.productdownloads
			]).then((purchase) => {
                setPurchase(purchase);
            }).catch(() => {
                setPurchase(undefined);
            });

   	},[]);

	if (!purchase) {
        return <Loading />;        
	}

    // if coming from checkout, show complete component to handle any status messages
	return <>
        <Complete noSessionUpdate={true} hideUnknownStatus={true} />
	</>;
};  

export default HostedStatus;