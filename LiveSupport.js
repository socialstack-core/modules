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
			mode: null,
			loading: props.startOpen
		};

		if(props.startOpen){
			this.start(props);
		}

		this.handleChatClose = this.handleChatClose.bind(this);
	}

	start(props){
		var args = {};

		if (props.mode) {
			args.mode = props.mode;
		}

		// We need to determine what mode we are going in with. If there is a complete chat identity, we need to change the mode and create that chat using those global values.
		if(global.app.state && global.app.state.chatIdentity && global.app.state.chatIdentity.fullName && global.app.state.chatIdentity.email) {
			if (props.mode) {
				if (props.mode == 1) {
					args.mode = 11;
				}
				if (props.mode == 2) {
					args.mode = 12;
				}
			}
			else {
				// There is no mode set, but there is a logged in user, so this is the default chat.
				args.mode = 10;
			}

			args.fullName = global.app.state.chatIdentity.fullName;
			args.email = global.app.state.chatIdentity.email;
		}

		webRequest('livesupportchat', args).then(response => {
			this.setState({
				loading: false,
				chat: response.json
			});
		})
	}

	handleChatClose() {
		this.setState({
			chat: false,
			loading: false,
			mode: null
		});

		global.app.setState({ chat: null });
	}

	renderStartChat(onClickActive) {
		var startClick = () => {
			this.setState({
				loading: true
			});
			this.start(this.props);
		};



		if(!onClickActive) {
			return this.props.children ? this.props.children() : <button type="button" className="btn">Chat with us</button>
		} else {
			return this.props.children ? this.props.children(startClick) : <button type="button" className="btn" onClick={startClick}>Chat with us</button>
		}


	}

	renderOpenChat() {
		var { title, closeImage, closeLabel, closeCallback, sendLabel, sendTip, placeholder } = this.props;
		var { mode } = this.state;

		console.log(title);
		title = title || "Chat";
		// TODO: default close image
		//closeImage = closeImage || ;
		closeLabel = closeLabel || "Close chat";

		return <div className="open-chat">
			<header className="chat-header">
				<span className="chat-title">{title}</span>
				<button type="button" className="btn chat-close-btn" title={closeLabel} aria-label={closeLabel} onClick={closeCallback || this.handleChatClose}>
					<img src={closeImage} alt="" role="presentation" />
				</button>
			</header>

			{/**Now we need to determine which menu to open. */}
			{this.state.loading ? <Loading /> : <MessageList onClose={this.handleChatClose} chat={this.state.chat} sendLabel={sendLabel} sendTip={sendTip} placeholder={placeholder} />}
		</div>;

	}

	render() {
		var {stayOpen} = this.props;
		var chatOpen = this.state.chat || this.state.loading;
		var supportClass = chatOpen && !stayOpen ? "livesupport open" : "livesupport";

		return <div className={supportClass}>
			{stayOpen ? (chatOpen ? [this.renderOpenChat(), this.renderStartChat(false)] : this.renderStartChat(true)) : chatOpen ? this.renderOpenChat() : this.renderStartChat(true) }
		</div>;

	}

}

LiveSupport.propTypes = {};
