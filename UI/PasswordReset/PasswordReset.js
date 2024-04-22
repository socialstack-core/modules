import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';
import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Text from 'UI/Text';
import { useState } from 'react';
import {useSession, useRouter} from 'UI/Session';
import {useTokens} from 'UI/Token';

export default function PasswordReset(props) {
	
	var {setPage} = useRouter();
	var {session, setSession} = useSession();
	var [loading, setLoading] = useState();
	var [failed, setFailed] = useState();
	var [policy, setPolicy] = useState();
	var [password, setPassword] = useState();

	var token = useTokens('${url.token}');
	
	function validatePasswordMatch(value) {
		if (password != value) {
			return {
				error: 'FORMAT',
				ui: <Text>{`The chosen passwords do not match`}</Text>
			};
		}
	}

    React.useEffect(() => {
		if(!token){
			return;
		}
		
		setLoading(true);
		
        webRequest("passwordresetrequest/token/" + token + "/").then(response => {
			setLoading(false);

			setFailed(!response.json || !response.json.token);
        }).catch(e => {
			setFailed(true);
        });
		
    }, []);
    
	return <div className="password-reset">
		{
			failed ? (
				<Alert type="error">
					{`Invalid or expired token. You'll need to request another one if this token is too old or was already used.`}
				</Alert>
			) : (
				loading ? (
					<div>
						<Loading />
					</div>
				) : (
					<Form
						successMessage={`Password has been set.`}
						failureMessage={`Unable to set your password. Your token may have expired.`}
						submitLabel={`Set my password`}
						action={"passwordresetrequest/login/" + token + "/"}
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
							setPolicy(null);
							return v;
						}}
						onFailed={e => {
							setPolicy(e);
						}}
					>

						<fieldset>
							<Input
								autocomplete="new-password"
								showMeter
								type='password'
								name='password'
								label={`New Password`}
								placeholder={`Enter new password`}
								validate={['Required', 'Password']}
								onChange={e => { setPassword(e.target.value); }} />
						</fieldset>

						<fieldset>
							<Input
								autocomplete="new-password"
								type='password'
								name='newPasswordConfirm'
								label={`Confirm Password`}
								placeholder={`Confirm password your new password`}
								validate={['Required', function (value) { return validatePasswordMatch(value) }]} />
						</fieldset>

						{policy && (
							<Alert type="error">
								{policy.message || `Unable to set your password - the request may have expired`}
							</Alert>
						)}
					</Form>
				)
			)
		}
	</div>;
}

PasswordReset.propTypes = {
	token: 'string',
	target: 'string'
};