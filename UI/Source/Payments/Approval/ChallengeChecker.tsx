import { useEffect } from "react";

import { ValidationMetaData } from './Challenge';
import purchaseApi from 'Api/Purchase';
import { PurchaseStatus } from 'Api/Payments';

type ChallengeCheckerProps = {
	metaData: ValidationMetaData | undefined;
	onChange?: (status: PurchaseStatus) => void;            
};

const ChallengeChecker: React.FC<ChallengeCheckerProps> = (props: ChallengeCheckerProps): React.ReactNode => {

	var { metaData, onChange } = props;

	useEffect(() => {
	
        if(!metaData) {
            return;
        }

		// calls the purchase controller to check the purchase status
		// should be updated via a callback from the provider 
		const update = () => {
			purchaseApi.approvalStatus(metaData?.token || null)
			.then((result: PurchaseStatus) => {
				console.log('Approval check ', result.status)
				
				if((result.status !== 250)) {
					onChange?.(result);
				}
			});
		};
		
		// call the above function
		update();

		// set an interval that calls update every 5 seconds
		const interval = setInterval(update, 5000);
		
        // when the component dismounts, clear the interval
		return () => clearInterval(interval);
	}, [metaData , onChange]);

return (
	<div>
		{`Authenticating, please wait...`}
	</div>
);

}

export default ChallengeChecker;