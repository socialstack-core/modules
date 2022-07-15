import Form from "UI/Form";
import Input from "UI/Input";
import Spacer from "UI/Spacer";
import Alert from "UI/Alert";
import {useSession, useRouter} from "UI/Session";

/**
 * Frontend register form.
 */

export default function RegisterForm(props){
	
	var {session, setSession} = useSession();
	var {setPage} = useRouter();
	
	return <RegisterFormIntl setSession={setSession} setPage={setPage} {...props}/>;
}


class RegisterFormIntl extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
	}
	
	render() {
		var {noUsername, noName, noLogin} = this.props;
		var {failed} = this.state;
		return (
			<Form
				action = "user"
				onSuccess={response => {
					if(this.props.autoLogin){
						// Requires a refresh as some internal state isn't returned by the endpoint.
						window.location = this.props.redirectTo || '/';
					}else{
						this.setState({success: true});
					}
				}}
				className="register-form"
				onFailed={e=>{
					console.log(e);
					this.setState({failed:e})
				}}
				onValues={values => {
					this.setState({ failed: false, success: false });

					return new Promise((success, reject) => {
						if (values.password && values.passwordRepeat && values.password == values.passwordRepeat) {
							success(values);
						} else {
							reject({
								message: `Passwords are required and must match`
							});
						}
					})

				}}
				>
                <p>
					{`All fields are required`}
                </p>
				<div>
					{!noName && <Input label={`First name`} name="firstName" placeholder={`Your first name`} validate={['Required']} />}
					{!noName && <Input label={`Last name`} name="lastName" placeholder={`Your last name`} validate={['Required']} />}
					<Input label={`Email`} name="email" placeholder={`Email address`} validate={['Required', 'EmailAddress']} />
					{!noUsername && <Input label={`Username`} name="username" placeholder={`Username`} validate={['Required']} /> }
					<Input label={`Password`} name="password" type="password" placeholder={`New Password`} validate={['Required']} />
					<Input label={`Password Repeat`} name="passwordRepeat" type="password" placeholder={`New Password Again`} validate={['Required']} />
				</div>
				{failed && (
					<Alert type="fail">
						{failed.message ? failed.message : failed == "VALIDATION" && `Please verify all values are correct.`}
					</Alert>
				)}
				{this.state.success ? (
					<Alert type="success">
						{`Account created! You can now`} <a href="/login">{`login here`}</a>.
					</Alert>
				) : (
						<div>
							<Spacer height="20"/>
							<Input type="submit" label={`Create my account`} />
							{!noLogin && <p>{`Already got an account?`} <a href="/login">{`Login here`}</a></p> }
						</div>
				)}
			</Form>
		);
	}
}

RegisterForm.propTypes = {
	noUsername: 'bool',
	noName: 'bool'
};
