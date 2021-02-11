import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';

/**
 * Frontend login form.
 */

export default class LoginForm extends React.Component {
	constructor(props){
		super(props);
		this.state={};
	}
	
	render() {
		var {emailOnly} = this.props;

		var validate = ['Required'];

		if (emailOnly) {
			validate.push("EmailAddress")
		}

		return (
			<Form className="login-form"
				action = "user/login" 
                onSuccess={response => {
					this.context.app.setState({user:response.user, realUser:response.user});
					if(!this.props.noRedirect){
						global.pageRouter.go("/");
					}
					this.props.onLogin && this.props.onLogin(response);
				}}
				onValues={v => {
					this.setState({failed:false})
					return v;
				}}
				onFailed={()=>{
					this.setState({failed:true})
				}}
				>
				<Input label = "Email" name="emailOrUsername" placeholder={emailOnly ? "Email" : "Email or username"} validate={validate} />
				<Input label = "Password" name="password" placeholder="Password" type="password" />
				<Row>
					<Col size="6">
						<label className="checkbox-label">
                            <input name="remember" type="checkbox" /> Remember me
						</label>
					</Col>
					<Col size="6" className="text-right">
						<a href="/forgot">I forgot my password</a>
					</Col>
				</Row>
				<Spacer height="20" />
				<Input type="submit" label="Login"/>
				{this.state.failed && (
					<Alert type="fail">
						Those login details weren't right - please try again.
					</Alert>
				)}
				<div className="form-group">
					Don't have an account? <a href="/register">Register here</a>
				</div>
			</Form>
		);
	}
}

LoginForm.propTypes = {
	emailOnly: 'bool'
};