import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';

/*
* Provide an email address in order to email a reset link to.
*/
export default class ForgotPassword extends React.Component {
	
	render() {
		return <div className="forgot-password">
				{
					this.state.success ? (
						<div>
							{this.props.successMessage || 'Your request has been submitted - if an account exists with this email address it will receive an email shortly.'}
						</div>
					) : [
						<p>
							{this.props.prompt || 'Please provide your email address and we\'ll email you a reset link.'}
						</p>,
						<Form
							failedMessage={this.props.failedMessage || "We weren't able to send the link. Please try again later."}
							submitLabel={this.props.submitLabel || "Send me a link"}
							loadingMessage={this.props.loadingMessage || "Sending.."}
							action="passwordresetrequest"
							onSuccess={response => {
								this.setState({
									success: true
								});
								this.props.onSuccess && this.props.onSuccess();
							}}
						>
							<Input name="email" placeholder="Email address" validate="Required" />
						</Form>
				]}
				{this.props.children || <div className="form-group">	
					<a className = "btn btn-primary" href={this.props.loginLink || "/login"}>Back to login</a>		
				</div>}
		</div>;
	}
}

ForgotPassword.propTypes={};