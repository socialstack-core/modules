import { useEffect, useRef } from 'react';
import { ValidationMetaData } from './Challenge';

type ChallengePageProps = {
	metaData: ValidationMetaData | undefined;
	onSubmitted?: () => void;
};

/**
 * Props:
 * - acsUrl: string (issuer ACS URL from Opayo response)
 * - cReq: string (base64 challenge payload from Opayo; post as 'creq')
 * - transactionId: string (3DS session data / transaction reference)
 * - onSubmitted?: () => void (optional callback after form submission)
 */

const ChallengePage: React.FC<ChallengePageProps> = (props: ChallengePageProps): React.ReactNode => {

	var { metaData, onSubmitted } = props;

	const formRef = useRef<HTMLFormElement | null>(null);

	useEffect(() => {
		if (!metaData) {
			return;
		}
		
		if (formRef.current) {
			formRef.current.submit();
			onSubmitted?.();
		}
	}, [metaData, onSubmitted]);

	return (
	<div className="purchase-challenge-page">
		{/* The ACS expects a POST with field named 'creq' (lowercase) */}
		<form ref={formRef} action={metaData?.challengeUrl} method="POST">
		<input type="hidden" name="creq" defaultValue={metaData?.challengeRequest} />
		<input type="hidden" name="threeDSSessionData" defaultValue={metaData?.sessionToken} />          
		<noscript>
			<p>{`3D Secure challenge requires JavaScript. Click the button below to continue.`}</p>
			<button type="submit">{`Continue`}</button>
		</noscript>
		</form>
	</div>
	);
}

export default ChallengePage;