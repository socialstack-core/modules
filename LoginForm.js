import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import { useSession, useRouter } from 'UI/Session';

/**
 * Frontend login form.
 */

export default function LoginForm (props) {
	var {emailOnly} = props;
	var { setSession } = useSession();
	var { setPage } = useRouter();
	var [failed,setFailed] = useState(false);
	
	var validate = ['Required'];

	if (emailOnly) {
		validate.push("EmailAddress")
	}

	return (
		<Form className="login-form"
			action = "user/login" 
			onSuccess={response => {
				setSession(response);
				if(!props.noRedirect){
					setPage("/");
				}
				props.onLogin && props.onLogin(response);
			}}
			onValues={v => {
				setFailed(false)
				return v;
			}}
			onFailed={()=>{
				setFailed(true)
			}}
			>
			<Input label = {emailOnly ? "Email" : "Email or username"}  name="emailOrUsername" placeholder={emailOnly ? "Email" : "Email or username"} validate={validate} />
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
			{failed && (
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