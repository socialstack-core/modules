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

	// Used to select the chat mode that we are about to enter.
	renderSelection() {
		var liveSelected = this.state.mode == "live";
		var questionSelected = this.state.mode == "question";
		var appointmentSelected = this.state.mode == "appointment";
		var expertSelected = this.state.mode == "expert";
		var disabled = this.state.mode != null ? "disabled" : null;

		return <div className="chat-selection">
			<div className="message">
				<p>Do you want help now?</p>
				<div className="chat-options">
					<Input key={"live"} className={liveSelected ? "btn btn-primary selected" : "btn btn-primary"} disabled={disabled} type="button"
						onClick={() => this.setState({ mode: "live" })}>
						Speak with a live operator
					</Input>
					<Input key={"question"} className={questionSelected ? "btn btn-primary selected" : "btn btn-primary"} disabled={disabled} type="button"
						onClick={() => this.setState({ mode: "question" })}>
						Do you have a question?
					</Input>
					<Input key={"appointment"} className={appointmentSelected ? "btn btn-primary selected" : "btn btn-primary"} disabled={disabled} type="button"
						onClick={() => this.setState({ mode: "appointment" })}>
						No, I want to book a 1:1 for later
					</Input>
					<Input key={"expert"} className={expertSelected ? "btn btn-primary selected" : "btn btn-primary"} disabled={disabled} type="button"
						onClick={() => this.setState({ mode: "expert" })}>
						Ask an expert
					</Input>
				</div>
			</div>
		</div>
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
			{!mode ? this.renderSelection() : this.state.loading ? <Loading /> : <MessageList chat={this.state.chat} sendLabel={sendLabel} sendTip={sendTip} placeholder={placeholder} />}

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