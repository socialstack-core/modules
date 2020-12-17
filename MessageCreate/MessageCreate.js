import Form from 'UI/Form';
import Input from 'UI/Input';
import Alert from 'UI/Alert';
import webRequest from 'UI/Functions/WebRequest';

export default class Create extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            messageText: ""
        };

        console.log("Message Create: ");
        console.log(props);
    }

    messageUpdated(e) {
        this.setState({
            messageText: e.target.value
        })
    }

    claimChat() {
        webRequest("livesupportchat/" + this.props.chat.id, { assignedToUserId: global.app.state.user.id }).then(response => {
            this.setState({ claimed: response });
            console.log("Chat was claimed!");
        }).catch(error => {
            console.log("There was an error claiming the chat.");
        })
    }

    render() {
        var { sendLabel, sendTip, placeholder } = this.props;

        sendLabel = sendLabel || "Send";
        sendTip = sendTip || "Send message";
        placeholder = placeholder || "Write your message here...";

        return <div className="message-create">
            <Form
                action="livesupportmessage"
                onSuccess={response => {
                    this.setState({ submitting: false, messageText: '' });
                    this.createTextarea && (this.createTextarea.value = '');
                }}
                onFailed={response => {
                    this.setState({ submitting: false, failure: response.message || 'Unable to send your message at the moment' });
                }}
                onValues={
                    values => {
                        console.log("submitting!");
                        values.inReplyTo = this.props.replyTo;
                        this.setState({ submitting: true, failure: false });
                        values.liveSupportChatId = this.props.chat.id;
                        return values;
                    }
                }
                className="message-form"
            >
                <div>
                    <Input onKeyPress={e => {
                        if (!this.state.submitting && this.state.messageText.trim().length > 0 && e.keyCode == 13) {
                            e.preventDefault();
                            e.target.form.submit();
                        }
                    }}
                        inputRef={e => this.createTextarea = e}
                        style={{ fontSize: "1em" }}
                        name="message"
                        type="textarea"
                        maxlength="1000"
                        autocorrect="off"
                        autocapitalize="off"
                        placeholder={placeholder}
                        label=""
                        noWrapper
                        onKeyUp={e => {
                            this.messageUpdated(e);
                        }} />
                    {this.state.failure && <Alert type="error">{this.state.failure}</Alert>}

                    {this.props.canClaim && !this.props.chat.assignedToUserId && !this.state.claimed &&
                        <a onClick={() => { console.log("Claimed!"); this.claimChat(); }} className="btn btn-primary send-message success" title="Claim">
                            <span>Claim!</span>
                        </a>
                    }
                    {this.props.canClaim && this.state.claimed && <Alert type="success">
                        You have successfully claimed this request! You are now in a one-on-one conversation with this user.
                    </Alert>}

                    <button className="btn btn-primary send-message" type="submit" disabled={this.state.submitting || this.state.messageText.trim().length === 0} title={sendTip}>
                        <span>{sendLabel}</span>
                    </button>
                </div>
            </Form>
        </div>;

    }

}
