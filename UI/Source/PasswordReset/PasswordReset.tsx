import Alert from 'UI/Alert';
import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';
import { useState, useEffect } from 'react';
import {useSession} from 'UI/Session';
import { useRouter } from 'UI/Router';
import passwordResetRequestApi, { NewPassword } from 'Api/PasswordResetRequest';

/**
 * Props for the password reset form.
 */
interface PasswordResetProps {
	token: string,
	onSuccess?:(s:SessionResponse)=>void
}

/**
 * A form used to reset a password using a provided token. 
 * Typically the token originates via the URL and is connected to this component with a graph.
 * @param props
 * @returns
 */
const PasswordReset: React.FC<PasswordResetProps> = (props) => {

	const { token } = props;

	var {setPage} = useRouter();
	var {setSession} = useSession();
	var [loading, setLoading] = useState(false);
	var [failed, setFailed] = useState<PublicError|null>(null);
	var [password, setPassword] = useState('');

	const validatePasswordMatch = (value : string): PublicError | undefined => {
		if (password != value) {
			return {
				type: 'password/no-match',
				message: `The chosen passwords do not match`
			};
		}
	}

    useEffect(() => {
		if(!token){
			return;
		}
		
		setLoading(true);

		passwordResetRequestApi.checkTokenExists(token)
			.then(response => {
				setLoading(false);
			})
			.catch(e => {
				setFailed(e as PublicError);
			});
		
	}, [token]);
    
	return <div className="password-reset">
		{
			loading ? (
				<div>
					<Loading />
				</div>
			) : (
				<Form
					successMessage={`Password has been set.`}
					failedMessage={`Unable to set your password. Your token may have expired.`}
					submitLabel={`Set My Password`}
					action={(np:NewPassword) => passwordResetRequestApi.loginWithToken(setSession, token, np)}
					onSuccess={response => {
						// Response is the new context.
						// Set to global state:
						setSession(response);
							
						if(props.onSuccess){
							props.onSuccess(response);
						}else{
							// Go to homepage:
							setPage('/');
						}
					}}
					onValues={v => {
						setFailed(null);
						return v;
					}}
					onFailed={e => {
						setFailed(e);
					}}
				>

					<fieldset>
						<Input
							autoComplete="new-password"							
							type='password'
							name='password'
							label={`New Password`}
							placeholder={`Enter new password`}
							validate={['Required', 'Password']}
							onChange={e => { setPassword((e.target as HTMLInputElement).value); }} />
					</fieldset>

					<fieldset>
						<Input
							autocomplete="new-password"
							type='password'
							name='newPasswordConfirm'
							label={`Confirm Password`}
							placeholder={`Confirm password your new password`}
							validate={['Required', validatePasswordMatch]} />
					</fieldset>

					{failed && (
						<Alert variant="danger">
							{failed.message || `Unable to set your password - the request may have expired`}
						</Alert>
					)}
				</Form>
			)
		}
	</div>;
}

export default PasswordReset;