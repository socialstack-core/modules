import Input from 'UI/Input';
import Form from 'UI/Form';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Canvas from 'UI/Canvas';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import { useSession, useRouter } from 'UI/Session';

/**
 * Admin login form.
 */

export default props => {	
	const { setSession } = useSession();
	const { setPage } = useRouter();
	const [ failed, setFailed ] = React.useState(false);
	const [ moreRequired, setMoreRequired ] = React.useState(null);
	
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
					setPage("/en-admin/");
				}
				props.onLogin && props.onLogin(response);
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