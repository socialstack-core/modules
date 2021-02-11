import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Canvas from 'UI/Canvas';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';

/**
 * Admin login form.
 */

export default class LoginForm extends React.Component {
	constructor(props){
		super(props);
		this.state={};
	}
	
	render() {
		var { failed } = this.state;
		
		return (
			<Form className="login-form"
				action = "user/login" 
                onSuccess={response => {
					if(response.moreDetailRequired){
						// More required - e.g. a 2FA screen.
						// The value of this is canvas compatible JSON.
						this.setState({
							moreRequired: response.moreDetailRequired
						});
						return;
					}
					
					this.context.app.setState(response);
					if(!this.props.noRedirect){
						global.pageRouter.go("/en-admin/");
					}
					this.props.onLogin && this.props.onLogin(response);
				}}
				onValues={v => {
					this.setState({failed:false})
					return v;
				}}
				onFailed={e=>{
					this.setState({failed:e})
				}}
				>
				{this.state.moreRequired && (
					<Canvas>{this.state.moreRequired}</Canvas>
				)}
				<div style={{display: this.state.moreRequired ? 'none' : 'initial'}}>
					<Input name="emailOrUsername" placeholder="Email or username" validate={['Required']} />
					<Input name="password" placeholder="Password" type="password" />
					<Row>
						<Col size="6">
							<label className="checkbox-label">
								<input name="remember" type="checkbox" /> Remember me
							</label>
						</Col>
						<Col size="6" className="text-right">
							<a href="/en-admin/forgot">I forgot my password</a>
						</Col>
					</Row>
				</div>
				<Spacer height="20" />
				<Input type="submit" label="Login"/>
				{failed && (
					<Alert type="fail">
						{failed.message || 'Those login details weren\'t right - please try again.'}
					</Alert>
				)}
				<div className="form-group">
					Don't have an account? <a href="/en-admin/register">Register here</a>
				</div>
			</Form>
		);
	}
}