import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Canvas from 'UI/Canvas';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import { useSession, useRouter } from 'UI/Session';

/**
 * Frontend login form.
 */

export default props => {
	const { setSession } = useSession();
	const { setPage } = useRouter();
	const [ failed, setFailed ] = React.useState(false);
	const [ moreRequired, setMoreRequired ] = React.useState(null);
	const {emailOnly, passwordRequired} = props;
	
	var validate = ['Required'];

	if (emailOnly) {
		validate.push("EmailAddress")
	}

	var validatePassword = [];
	if (passwordRequired) {
		validatePassword.push('Required');
	}

	return (
		<Form className="login-form"
			action = "user/login" 
			onSuccess={response => {
				if(response.moreDetailRequired){
					// More required - e.g. a 2FA screen.
					// The value of this is canvas compatible JSON.
					setMoreRequired(response.moreDetailRequired);
					return;
				}
				
				setSession(response);
				
				if(!props.noRedirect){
					// If there is a then arg in the url, redirect to that.
					if(location.search){
						var args = {};
						var pieces = location.search.substring(1).split('&');
						for(var i=0;i<pieces.length;i++){
							var queryPart = pieces[i].split('=', 2);
							args[queryPart[0]] = queryPart.length == 2 ? decodeURIComponent(queryPart[1]) : true;
						}
						
						// The provided URL must be relative to site root only.
						if(args.then && args.then.length>1 && args.then[0] == '/' && args.then[1] != '/'){
							setPage(args.then);
							return;
						}
					}
					
					setPage(props.redirectTo || '/');
				}
				props.onLogin && props.onLogin(response, setPage, setSession, props);
			}}
			onValues={v => {
				setFailed(false);
				return v;
			}}
			onFailed={e=>setFailed(e)}
			>
			{moreRequired && (
				<Canvas>{moreRequired}</Canvas>
			)}
			<div style={{display: moreRequired ? 'none' : 'initial'}}>
				<Input label = {props.noLabels ? null : (emailOnly ? `Email` : `Email or username`)}  name="emailOrUsername" placeholder={emailOnly ? `Email` : `Email or username`} validate={validate} />
				<Input label = {props.noLabels ? null : `Password`} name="password" placeholder={`Password`} type="password" validate = {validatePassword} />
				<Row>
					{!props.noRemember && <Col size="6">
						<Input type="checkbox" label={`Remember me`} name="remember" />
					</Col>}
					<Col size="6">
					{!props.noForgot && (
						<a href="/forgot" className="forgot-password-link">
							{props.forgotPasswordText || `I forgot my password`}
						</a>
					)}
					</Col>
				</Row>
			</div>
			<Spacer height="20" />
			<Input type="submit" label={props.loginCta || `Login`}/>
			{failed && (
				<Alert type="fail">
					{failed.message || `Those login details weren't right - please try again.`}
				</Alert>
			)}
			{props.noRegister ? null : <div className="form-group">
				<span className="fa fa-info-circle"></span> {`Don't have an account?`} <a href={props.registerUrl || "/register"}>{`Register here`}</a>
			</div>}
		</Form>
	);
}