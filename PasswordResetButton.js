import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';


export default class PasswordResetButton extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
	}
	
	generate(){
		webRequest('passwordresetrequest/' + this.props.userId + '/generate').then(response => {
			
			var relativeUrl = response.json.url;
			var url = global.location.origin + relativeUrl;
			
			this.setState({
				loading: false,
				url
			});
		})
	}
	
	render(){
		
		return <div className="password-reset-button">
			<button className="btn btn-secondary" onClick={() => this.generate()}
				disabled={this.state.loading}
			>
				Generate password reset link
			</button>
			{this.state.url && (
				<div>
					<Alert type="info">
						Send this to the user - when they open it in a browser, they'll be able to set a password and login. 
					</Alert>
					<p>
						{this.state.url}
					</p>
				</div>
			)}
		</div>;
		
	}
	
}

PasswordResetButton.propTypes = {
	userId: 'int'
};