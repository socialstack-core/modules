import {useTokens} from 'UI/Token';
import Form from "UI/Form";
import Input from "UI/Input";
import Spacer from "UI/Spacer";
import Alert from "UI/Alert";
import { useRouter } from 'UI/Session';

export default function VerifyEmail(props) {
	const { onSuccess } = props;
	var [failed, setFailed] = React.useState();
	var [success, setSuccess] = React.useState();

	var token = useTokens('${url.token}');
	var userId = useTokens('${url.userid}');

	return (			
		<Form
			action = {"user/verify/" + userId + "/" + token}
			method='POST'
			onSuccess={response => {
				setSuccess(true);
				onSuccess && onSuccess();
			}}
			className="verify-email-form"
			onFailed={e=>{
				if (!e.message) {
					e.message = "Something went wrong. Please make sure the passwords match and try again.";
				}
				setFailed(e);
			}}
			onValues={v=>{
				if (v.password !== v.passwordRepeat) {
					return Promise.reject(new Error('The passwords do not match.'));
				}

				return v;
			}}
		>
			<h2 className="verify-header">Create a password</h2>

			<div className="verify-description">
				<p>
					In order to complete registration, you will need to create a password.
				</p>
			</div>

			<div className="verify-input-group">
				<Input className="verify-input input-grey" name="password" type="password" placeholder="Create a password" validate={['Required']} onInput/>
				<Input className="verify-input input-grey" name="passwordRepeat" type="password" placeholder="Confirm password" validate={['Required']} onInput/>
				<Input className="remember-me" name="rememberMe" type="checkbox" label={"Remember me"} />
			</div>
			{failed && (
				<Alert type="fail">
					{failed.message ? failed.message : failed == "VALIDATION" && "Please verify all values are correct."}
				</Alert>
			)}
			{success ? (
				<Alert type="success">
					Account created!
				</Alert>
			) : (
				<div>
					<Spacer height="20"/>
					<Input className="btn btn-primary verify-input" type="submit" label="Next" />
				</div>
			)}
		</Form>
	);
}


VerifyEmail.propTypes = {
};

// use defaultProps to define default values, if required
VerifyEmail.defaultProps = {
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
VerifyEmail.icon='user-check'; // fontawesome icon
