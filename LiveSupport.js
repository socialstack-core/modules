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
			chat: null,
			mode: null
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

		return this.props.children ? this.props.children(startClick) : <button type="button" className="btn" onClick={startClick}>Chat with us</button>
	}

	renderOpenChat() {
		var { title, closeImage, closeLabel, sendLabel, sendTip, placeholder } = this.props;
		var { mode } = this.state;

		title = title || "Chat";
		// TODO: default close image
		//closeImage = closeImage || ;
		closeLabel = closeLabel || "Close chat";		

		return <div className="open-chat">
			<header className="chat-header">
				<span className="chat-title">{title}</span>
				<button type="button" className="btn chat-close-btn" title={closeLabel} aria-label={closeLabel} onClick={() => {
					this.setState({
						chat: false,
						loading: false,
						mode: null
					});
				}}>
					<img src={closeImage} alt="" role="presentation" />
				</button>
			</header>

			{/**Now we need to determine which menu to open. */}
			{this.state.loading ? <Loading /> : <MessageList onClose = {() => {
					this.setState({
						chat: false,
						loading: false,
						mode: null
					});
				}} chat={this.state.chat} sendLabel={sendLabel} sendTip={sendTip} placeholder={placeholder} />}

		</div>;

	}

	render() {
		var chatOpen = this.state.chat || this.state.loading;
		var supportClass = chatOpen ? "livesupport open" : "livesupport";

		return <div className={supportClass}>
			{chatOpen ? this.renderOpenChat() : this.renderStartChat()}
		</div>;

	}

}

LiveSupport.propTypes = {};