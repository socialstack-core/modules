import Loop from 'UI/Loop';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import webRequest from 'UI/Functions/WebRequest';
import MessageList from 'UI/LiveSupport/MessageList';


export default class LiveSupport extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = {
			chat: null
		};
	}
	
	renderStartChat() {
		var startClick = () => {
			this.setState({
				loading: true
			});
			webRequest('livesupportchat', {}).then(response => {
				this.setState({
					loading: false,
					chat: response.json
				});
			})
		};
		
		return this.props.children ? this.props.children(startClick) : <button onClick={startClick}>Chat with us</button>
	}
	
	renderOpenChat() {
		
		return <div className="open-chat">
			{this.state.loading ? <Loading /> : <MessageList chat={this.state.chat}/>}
		</div>;
		
	}
	
	render() {
		
		return <div className="livesupport">
			{(this.state.chat || this.state.loading) ? this.renderOpenChat() : this.renderStartChat()}
		</div>;
		
	}
	
}

LiveSupport.propTypes = {};