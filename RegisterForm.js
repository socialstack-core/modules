import Form from "UI/Form";
import Input from "UI/Input";
import Spacer from "UI/Spacer";
import Alert from "UI/Alert";

/**
 * Admin register form.
 */

export default class RegisterForm extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
	}
	
	render() {
		return (
			<Form
				action = "user"
				onSuccess={response => {
					this.setState({success: true})
				}}
				className="register-form"
				>
                <p>
                    All fields are required
                </p>
				<div>
					<Input name="firstName" placeholder="Your first name" validate={['Required']} />
					<Input name="lastName" placeholder="Your last name" validate={['Required']} />
					<Input name="email" type="email" placeholder="Email address" validate={['Required', 'EmailAddress']} />
					<Input name="password" type="password" placeholder="New Password" validate={['Required']} />
				</div>
				{this.state.success ? (
					<Alert type="success">
						Account created! Please ask an existing admin to enable it for you.
					</Alert>
				) : (
					<div>
						You'll need to ask to be authorised.
						<Spacer height="20"/>
						<Input type="submit" label="Create my account" />
						Already got an account? <a href="/en-admin/login">Login here</a>
					</div>
				)}
			</Form>
		);
	}
}
