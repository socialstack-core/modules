import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';
import Loading from 'UI/Loading';
import Form from 'UI/Form';
import Input from 'UI/Input';


export default class PasswordReset extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {
		};
	}
	
    componentDidMount(){
        this.load(this.props);
    }
    
	componentWillReceiveProps(props){
        this.load(props);
    }
    
    load(props){
		const token = props.token;
		if(!token){
			return;
		}
		
        this.setState({token, loading: true});
        
        webRequest("passwordresetrequest/token/" + token + "/").then(response => {
            this.setState({loading: false, failed: !response.json || !response.json.token});
        }).catch(e => {
            this.setState({failed: true});
        })
    }
    
	render(){
		var {policy} = this.state;
		
		return <div className="password-reset">
			{
                this.state.failed ? (
                    <Alert type="error">
						Invalid or expired token. You'll need to request another one if this token is too old or was already used.
                    </Alert>
                ) : (
                    this.state.loading ? (
                        <div>
                            <Loading />
                        </div>
                    ) : (
                        <Form
							successMessage="Password has been set."
							failureMessage="Unable to set your password. Your token may have expired."
							submitLabel="Set my password"
                            action={"passwordresetrequest/login/" + this.props.token + "/"}
							onSuccess={response => {
								// Response is the new context.
								// Set to global state:
								global.app.setState(response);
								
								if(this.props.onSuccess){
									this.props.onSuccess(response);
								}else{
									// Go to homepage:
									global.pageRouter.go('/');
								}
							}}
							onValues={v => {
								this.setState({policy: null});
								return v;
							}}
							onFailed={e => {
								this.setState({policy: e});
							}}
                        >
                            <Input name="password" type="password" placeholder="Your password" />
							{policy && (
								<Alert type="error">{
									policy.message || 'Unable to set your password - the request may have expired'
								}</Alert>
							)}
                        </Form>
                    )
                )
            }
		</div>;
		
	}
	
}

PasswordReset.propTypes = {
	token: 'string',
	target: 'string'
};