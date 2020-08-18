import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';


export default class TwoFactorGoogleSetup extends React.Component {
	
	/*
	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
		};
	}
	*/
	
	render(){
		
		var {user} = global.app.state;
		
		if(!user){
			return <Loading />;
		}
		
		return <div className="two-factor-google-setup">
			<h2>
				Two factor authentication setup
			</h2>
			<p>
				To setup two factor authentication, you'll need the Google Authenticator app on your phone. Once you've installed that, press the add button and scan the QR code below:
			</p>
			<img src={'/v1/user/setup2fa/newkey'} />
			<p>
				When you've added it, enter the 6 digit pin to confirm:
			</p>
			<Form
				action="user/setup2fa/confirm"
				failureMessage="Unfortunately that pin was incorrect, so two factor authentication has not been enabled."
				successMessage="Two factor authentication has been enabled"
				submitLabel="Setup 2FA"
			>
				<Input type="text" name="pin" />
			</Form>
		</div>;
		
	}
	
}

TwoFactorGoogleSetup.propTypes = {
};