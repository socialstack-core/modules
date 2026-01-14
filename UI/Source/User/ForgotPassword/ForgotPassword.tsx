import Input from 'UI/Input';
import Form from 'UI/Form';
import Link from 'UI/Link';
import { useState } from 'react';
import resetApi, { PasswordResetRequest } from 'Api/PasswordResetRequest';

/**
 * Props for the forgot password form.
 */
interface ForgotPasswordProps {
	successMessage? : React.ReactNode,
	failedMessage? : React.ReactNode,
	submitLabel? : string,
	loginLink? : string,
	loadingMessage? : string,
	prompt?: React.ReactNode,
	onSuccess?: (response: PasswordResetRequest) => void
}

/**
* Provide an email address in order to email a reset link to.
*/
const ForgotPassword: React.FC<React.PropsWithChildren<ForgotPasswordProps>> = ({
	children,
	successMessage,
	prompt,
	failedMessage,
	submitLabel,
	onSuccess,
	loginLink,
	loadingMessage
}) => {
	const [success, setSuccess] = useState(false);

	return <div className="forgot-password">
		{
			success ? (
				<div>
					{successMessage || `Your request has been submitted - if an account exists with this email address it will receive an email shortly.`}
				</div>
			) : [
				<Form
					failedMessage={failedMessage || `We weren't able to send the link. Please try again later.`}
					submitLabel={submitLabel || `Send me a link`}
					loadingMessage={loadingMessage || `Sending..`}
					action={resetApi.create}
					onSuccess={response => {
						setSuccess(true);
						onSuccess && onSuccess(response);
					}}
				>
					<Input label={prompt || `Please provide your email address and we'll email you a reset link.`}
						type={'email'} name="email" placeholder={`Email address`} validate={["Required"]} />
				</Form>
		]}
		{children || <div className="form-group">
			<Link outlined href={loginLink || "/login"}>
				{`Back to login`}
			</Link>
		</div>}
	</div>;
}

export default ForgotPassword;