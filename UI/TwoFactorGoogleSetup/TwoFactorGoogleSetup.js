import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';
import QrCode from 'UI/QrCode';
import {SessionConsumer} from 'UI/Session';

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
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}

	renderIntl(session) {
		
		if(this.props.loginForm){
			if(this.props.setupUrl){
				// Setup form required here:
				return <div>
					<h3>
						{`Two factor authentication required`}
					</h3>
					<p>
						{this.props.introText}
					</p>
					<QrCode text={this.props.setupUrl} width={256} height={256} />
					<br/>
					<p>
						{`When you've added it, enter the 6 digit pin to confirm:`}
					</p>
					<Input name="google2FAPin" validate={['Required']} />
				</div>;
			}else{
				return <div>
					<h3>
						{`Two factor authentication`}
					</h3>
					<p>
						{`Please provide the auth code from your device:`}
					</p>
					<Input name="google2FAPin" validate={['Required']} />
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
				{this.props.introText}
			</p>
			<QrCode text={this.props.setupUrl} width={256} height={256} />
			<br />
			<p>
				{`When you've added it, enter the 6 digit pin to confirm:`}
			</p>
			<Form
				action="user/setup2fa/confirm"
				failureMessage={`Unfortunately that pin was incorrect, so two factor authentication has not been enabled.`}
				successMessage={`Two factor authentication has been enabled`}
				submitLabel={`Setup 2FA`}
			>
				<Input type="text" name="pin" />
			</Form>
		</div>;
		
	}
	
}

TwoFactorGoogleSetup.propTypes = {
	introText: 'string'
};

TwoFactorGoogleSetup.defaultProps = {
	introText: `To setup two factor authentication, you'll need the Google Authenticator app on your phone. Once you've installed that, press the add button and scan the QR code below:`
};