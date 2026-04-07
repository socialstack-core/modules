import { useEffect, useRef } from "react";
import { ValidationMetaData } from 'Api/ShoppingCart';

type ChallengeProps = {
    metaData: ValidationMetaData | undefined;
	width?: number | string;
	height?: number | string; 
	onSubmitted?: () => void;               // called when the form is submitted
	onError?: (err: Error) => void;
};

/**
 * Props:
 * - acsUrl: string (issuer ACS URL from Opayo response)
 * - cReq: string (base64 challenge payload from Opayo; post as 'creq')
 * - transactionId: string (3DS session data / transaction reference)
 * - onSubmitted?: () => void (optional callback after form submission)
 * - width?: number|string (e.g., 400 or '100%')
 * - height?: number|string (e.g., 600 or '100%')
 * - onSubmitted?: () => void (optional callback after form submission)
 * - onError?: (err: Error) => void (optional callback on submission error)
 */


const Challenge: React.FC<ChallengeProps> = (props: ChallengeProps): React.ReactNode => {

	const { metaData, width, height, onSubmitted, onError } = props;

	const formRef = useRef<HTMLFormElement | null>(null);
	// unique iframe name per component instance

	const iframeNameRef = useRef(`3DSChallengeFrame_${Math.random().toString(36).slice(2, 9)}`);
	const iframeRef = useRef<HTMLIFrameElement | null>(null);

	useEffect(() => {
		// don't attempt submission without required fields
		if (!metaData) {
			return;
		}
		
		// small delay ensures refs are mounted (safe for SSR hydration edge cases)
		const t = window.setTimeout(() => {
			if (formRef.current) {
				try {
					formRef.current.submit();
					onSubmitted?.();
				} catch (err) {
					onError?.(err instanceof Error ? err : new Error(String(err)));
				}
			} else {
				onError?.(new Error("Form element not available to submit"));
			}
		}, 0);

		return () => {
			clearTimeout(t);
		};
	}, [metaData, onSubmitted, onError]);

	return (
		<div className="payment-challenge__wrapper">
			<iframe
				ref={iframeRef}
				name={iframeNameRef.current}
				title="3D Secure Challenge"
				style={{ border: "1px solid #ccc" }}
				width={width}
				height={height}
				allow="fullscreen; payment"
			/>
			<form	
				ref={formRef}
				action={metaData?.challengeUrl}
				method="POST"
				target={iframeNameRef.current}
				style={{ display: "none" }}
			>
				<input type="hidden" name="creq" defaultValue={metaData?.challengeRequest} />
				<input type="hidden" name="threeDSSessionData" defaultValue={metaData?.sessionToken } />				
			</form>
		</div>
	);
}

export default Challenge;