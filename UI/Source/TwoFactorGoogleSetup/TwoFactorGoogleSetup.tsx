import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';
import QrCode from 'UI/QrCode';
import {useSession} from 'UI/Session';
import userApi from 'Api/User';
import { useState } from 'react';
import { PublicError } from 'UI/Failed';

/**
 * Props for the two factor google setup
 */
interface TwoFactorGoogleSetupProps {
	loginForm: boolean,
	setupUrl: string,
	introText: string,
	onSuccess?:(s:SessionResponse)=>void
}

const defaultIntroText = `To setup two factor authentication, you'll need the Google Authenticator app on your phone. Once you've installed that, press the add button and scan the QR code below:`

const TwoFactorGoogleSetup: React.FC<TwoFactorGoogleSetupProps> = ({
	loginForm,
	setupUrl,
	introText = defaultIntroText,
	onSuccess
}) => {
	var {session} = useSession();
	const [ failed, setFailed ] = useState<PublicError | null>(null);
		
	if(loginForm){
		if(setupUrl){
			// Setup form required here:
			return <div>
				<h3>
					{`Two factor authentication required`}
				</h3>
				<p>
					{introText}
				</p>
				<QrCode text={setupUrl} width={256} height={256} />
				<br/>
				<p>
					{`When you've added it, enter the 6 digit pin to confirm:`}
				</p>
				<Input name="google2FAPin" validate={['Required']} type="number"/>
			</div>;
		}else{
			return <div>
				<h3>
					{`Two factor authentication`}
				</h3>
				<p>
					{`Please provide the auth code from your device:`}
				</p>
				<Input name="google2FAPin" validate={['Required']} type="number"/>
			</div>;
		}
	}
	
	var {user} = session;
	
	if(!user){
		return <Loading />;
	}
	
	return <div className="two-factor-google-setup">
		<h2>
			{`Two factor authentication setup`}
		</h2>
		<p>
			{introText}
		</p>
		<QrCode text={setupUrl} width={256} height={256} />
		<br />
		<p>
			{`When you've added it, enter the 6 digit pin to confirm:`}
		</p>
		<Form
			action={userApi.twoFactorSetup}
			onFailed={(e) => {
				setFailed(new PublicError(`2fa_failed`, `Unfortunately that pin was incorrect, so two factor authentication has not been enabled.`));
			}}
			// onSuccess={() => {
			// 	onSuccess(`Two factor authentication has been enabled`)
			// }}
			// {successMessage={`Two factor authentication has been enabled`}}
			submitLabel={`Setup 2FA`}
		>
			<Input type="text" name="pin" />
		</Form>
	</div>;
	
}

export default TwoFactorGoogleSetup;