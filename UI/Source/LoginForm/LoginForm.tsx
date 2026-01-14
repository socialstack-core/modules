import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Canvas from 'UI/Canvas';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import Icon from 'UI/Icon';
import { useSession } from 'UI/Session';
import { useRouter } from 'UI/Router';
import { useState, useEffect } from 'react';
import userApi from 'Api/User';

export interface LoginFormProps {
	passwordRequired?: boolean,
	emailOnly?: boolean,
	flipButtons?: boolean,
	noRemember?: boolean,
	noForgot?: boolean,
	noLabels?: boolean,
	noRedirect?: boolean,
	noRegister?: boolean,
	forgotPasswordText?: string,
	loginCta?: string,
	registerUrl?: string,
	redirectTo?: string,
	onLogin?: (r: SessionResponse, setPage: (url: string) => void, setSession: (s: SessionResponse) => void, args: LoginFormProps) => void
}

/**
 * Frontend login form.
 */

export default (props : LoginFormProps) => {
	const { session, setSession } = useSession();

	const router = useRouter();

	if (!router) {
		return;
	}

	const { setPage } = router;

	const [ failed, setFailed ] = useState<PublicError | null>(null);
	const [ moreRequired, setMoreRequired ] = useState<string|null>(null);
	const [ emailVerificationRequired, setEmailVerificationRequired ] = useState(false);
	const [emailVerificationSent, setEmailVerificationSent] = useState(false);
	const {emailOnly, passwordRequired} = props;
	const user = session.user;

	var validate = ['Required'];

	if (emailOnly) {
		validate.push("EmailAddress")
	}

	var validatePassword : string[] = [];
	if (passwordRequired) {
		validatePassword.push('Required');
	}

	var onClickResendVerificationEmail = () => {
		userApi.resendVerificationEmail(setSession, {
			email: user?.email
		}).then(() => {
			setEmailVerificationSent(true);
		});
	}

	var renderFormFields = () => {
		var loginBtnStyle: React.CSSProperties = props.flipButtons ? { float: "right" } : {};
		var loginBtn = <Input type="submit" style={loginBtnStyle} label={props.loginCta || `Login`}/>;

		var rememberChkBoxStyle: React.CSSProperties = props.flipButtons ? { float: "right" } : { };
		var rememberChkBox = <Input type="checkbox" style={rememberChkBoxStyle} label={`Remember me`} name="remember" />;

		var forgotLinkStyle: React.CSSProperties = props.flipButtons ? { textAlign: 'left' } : {};
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
				<Input label = {props.noLabels ? null : (emailOnly ? `Email` : `Email or username`)}  name="emailOrUsername" placeholder={emailOnly ? `Email` : `Email or username`} type={emailOnly ? 'email' : 'text'} validate={validate} />
				<Input label = {props.noLabels ? null : `Password`} name="password" placeholder={`Password`} type="password" validate = {validatePassword} />
				{row}
			</div>
			<Spacer height={ 20 } />
			{afterRow}
		</>)
	}

	useEffect(() => {
		if (user && user.role == 3) {
			setEmailVerificationRequired(true);
		}
	}, [user]);

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
			action={ userApi.login } 
			onSuccess={response => {
				if(response.moreDetailRequired){
					// More required - e.g. a 2FA screen.
					// The value of this is canvas compatible JSON.
					setMoreRequired(response.moreDetailRequired);
					return;
				}
				
				setSession(response);

				if (response.role?.result?.id == 3)
				{
					setEmailVerificationRequired(true);
				}
				else if(!props.noRedirect){
					// If there is a then arg in the url, redirect to that.
					if(location.search){
						var args = new URLSearchParams(location.search);
						var targetUrl = args.get('then');

						// The provided URL must be relative to site root only.
						if (targetUrl && targetUrl.length > 1 && targetUrl[0] == '/' && targetUrl[1] != '/'){
							setPage(targetUrl);
							return;
						}
					}
					
					setPage(props.redirectTo || '/');
				}
				props.onLogin && props.onLogin(response, setPage, setSession, props);
			}}
			onValues={v => {
				setFailed(null);
				return v;
			}}
			onFailed={e=>setFailed(e)}
			>
			{moreRequired && (
				<Canvas>{moreRequired}</Canvas>
			)}
			{renderFormFields()}
			{failed && (
				<Alert variant="danger">
					{failed.message || `Those login details weren't right - please try again.`}
				</Alert>
			)}
			{props.noRegister ? null : <div className="form-group">
				<Icon type="fa-info-circle" /> {`Don't have an account?`} <a href={props.registerUrl || "/register"}>{`Register here`}</a>
			</div>}
		</Form>
	);
}