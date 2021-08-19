import webRequest from 'UI/Functions/WebRequest';
import {useTokens} from 'UI/Token';
import Alert from 'UI/Alert';
import Loading from 'UI/Loading';


export default function PasswordResetButton (props) {
	var [loading, setLoading] = React.useState();
	var [url, setUrl] = React.useState();
	var userId = useTokens('${url.user.id}');
	
	function generate(){
		setLoading(true);
		webRequest('passwordresetrequest/' + userId + '/generate').then(response => {
			var relativeUrl = response.json.url;
			var url = location.origin + relativeUrl;
			
			setUrl(url);
			setLoading(false);
		})
	}
	
	return <div className="password-reset-button">
		{loading ? (
			<Loading />
		) : <>
			<button className="btn btn-secondary" onClick={() => generate()}
				disabled={loading}
			>
				Generate password reset link
			</button>
			{url && (
				<div>
					<Alert type="info">
						Send this to the user - when they open it in a browser, they'll be able to set a password and login. 
					</Alert>
					<p>
						{url}
					</p>
				</div>
			)}
			</>
		}
	</div>;
}

PasswordResetButton.propTypes = {
	userId: 'int'
};