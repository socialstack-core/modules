import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Canvas from 'UI/Canvas';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import { useSession, useRouter } from 'UI/Session';
import { useState, useEffect } from 'react';
import webRequest from 'UI/Functions/WebRequest';

/**
 * Frontend login form.
 */

export default props => {
	const { session, setSession } = useSession();
	const { setPage } = useRouter();
	const [ failed, setFailed ] = useState(false);
	const [ moreRequired, setMoreRequired ] = useState(null);
	const [ emailVerificationRequired, setEmailVerificationRequired ] = useState(null);
	const [ emailVerificationSent, setEmailVerificationSent ] = useState(null);
	const {emailOnly, passwordRequired} = props;
	const user = session.user;

	var validate = ['Required'];

	if (emailOnly) {
		validate.push("EmailAddress")
	}

	var validatePassword = [];
	if (passwordRequired) {
		validatePassword.push('Required');
	}

	onClickResendVerificationEmail = () => {
		webRequest('user/sendverifyemail', { email: user.email }).then(resp => {
			setEmailVerificationSent(true);
		});
	}

	renderFormFields = () => {
		var loginBtnStyle = props.flipButtons ? "float: right" : "";
		var loginBtn = <Input type="submit" style={loginBtnStyle} label={props.loginCta || `Login`}/>;

		var rememberChkBoxStyle = props.flipButtons ? "float: right" : "";
		var rememberChkBox = <Input type="checkbox" style={rememberChkBoxStyle} label={`Remember me`} name="remember" />;

		var forgotLinkStyle = props.flipButtons ? "text-align: left" : "";
		var forgotLink = <a href="/forgot" style={forgotLinkStyle} className="forgot-password-link">{props.forgotPasswordText || `I forgot my password`}</a>;

		var col1Comp = (props.noRemember) ? loginBtn : rememberChkBox;
		var col2Comp = (props.noForgot) ? <></> : forgotLink;
		if (props.flipButtons) {
			col1Comp = (props.noForgot) ? <></> : forgotLink;
			col2Comp = (props.noRemember) ? loginBtn : rememberChkBox;
		}

		var row = (
			<Row>
				<Col sizeXs="6">
					{col1Comp}
				</Col>
				<Col sizeXs="6">
					{col2Comp}
				</Col>
			</Row>
		);

		var afterRow;
		if (!props.noRemember) {
			afterRow = loginBtn;
		}

		return (<>
			<div style={{display: moreRequired ? 'none' : 'initial'}}>
				<Input label = {props.noLabels ? null : (emailOnly ? `Email` : `Email or username`)}  name="emailOrUsername" placeholder={emailOnly ? `Email` : `Email or username`} validate={validate} />
				<Input label = {props.noLabels ? null : `Password`} name="password" placeholder={`Password`} type="password" validate = {validatePassword} />
				{row}
			</div>
			<Spacer height="20" />
			{afterRow}
		</>)
	}

	useEffect(() => {
		if (user && user.Role == 3) {
			setEmailVerificationRequired(true);
		}
	});

	if (emailVerificationRequired) {
		return <div className="login-form">
			<p>`You need to verify your email to continue. Please follow the instructions in the email, or you can resend the email by pressing the button below.`</p>
			{!emailVerificationSent
				? 
					<button className="btn btn-primary" onClick={e => onClickResendVerificationEmail()}>
						`Resend email`
					</button>
				: 
					<p>`Email sent!`</p>
			}
		</div>;
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

				if (response && response.role && response.role.id == 3 || response.role.key == "guest")
				{
					setEmailVerificationRequired(true);
				}
				else if(!props.noRedirect){
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
			{renderFormFields()}
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