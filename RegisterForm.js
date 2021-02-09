import Form from "UI/Form";
import Input from "UI/Input";
import Spacer from "UI/Spacer";
import Alert from "UI/Alert";

/**
 * Frontend register form.
 */

export default class RegisterForm extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
	}
	
	render() {
		var {noUsername} = this.props;
		var {failed} = this.state;
		return (
			<Form
				action = "user"
				onSuccess={response => {
					this.setState({success: true})
				}}
				className="register-form"
				onFailed={e=>{
					console.log(e);
					this.setState({failed:e})
				}}
				onValues = {values => {
					this.setState({failed: false, success: false});
					return values;
				}}
				>
                <p>
                    All fields are required
                </p>
				<div>
					<Input name="firstName" placeholder="Your first name" validate={['Required']} />
					<Input name="lastName" placeholder="Your last name" validate={['Required']} />
					<Input name="email" placeholder="Email address" validate={['Required', 'EmailAddress']} />
					{!noUsername && <Input name="username" placeholder="Username" validate={['Required']} /> }
					<Input name="password" type="password" placeholder="New Password" validate={['Required']} />
					<Input name="passwordRepeat" type="password" placeholder="New Password Again" validate={['Required']} />
				</div>
				{failed && (
					<Alert type="fail">
						{failed.message ? failed.message : failed == "VALIDATION" && "Please verify all values are correct."}
					</Alert>
				)}
				{this.state.success ? (
					<Alert type="success">
						Account created! You can now <a href="/login">login here</a>.
					</Alert>
				) : (
					<div>
						<Spacer height="20"/>
						<Input type="submit" label="Create my account" />
						Already got an account? <a href="/login">Login here</a>
					</div>
				)}
			</Form>
		);
	}
}

RegisterForm.propTypes = {
	noUsername: 'bool'
};
