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

    closeChat() {
        webRequest("livesupportchat/" + this.props.chat.id, { assignedToUserId: null, enteredQueueUtc: null}).then(response => {
            console.log("Chat was closed!");
        }).catch(error => {
            console.log("There was an error claiming the chat.");
        })
    }

    componentWillReceiveProps(newProps) {
        if(newProps.lastMessage && newProps.lastMessage.messageType && newProps.lastMessage.messageType == 2) {
            this.setState({canDownload: true})
        }
    }

    render() {
        var { sendLabel, sendTip, placeholder, lastMessage } = this.props;
        var {canDownload} = this.state;

        sendLabel = sendLabel || "Send";
        sendTip = sendTip || "Send message";
        placeholder = placeholder || "Write your message here...";
        var validate = [];
        // Let's look at the last Message's Message Type to see if we need to apply any validation. 
        if(lastMessage && lastMessage.messageType){
            if (lastMessage.messageType == 3) {
                validate.push("FullName");
            }

            if (lastMessage.messageType == 4) {
                validate.push("EmailAddress");
            }

            if (lastMessage.messageType == 5) {
                validate.push("PhoneNumber");
            }

            // Lastly, let's check if our close condition hit.
            if (lastMessage.messageType == 6) {
                this.props.onClose && this.props.onClose();
            }
        }

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
                        validate = {validate}
                        onKeyUp={e => {
                            this.messageUpdated(e);
                        }} />

                    {this.props.canClaim && !this.props.chat.assignedToUserId &&
                        <a onClick={() => { console.log("Claimed!"); this.claimChat(); }} className="btn btn-primary send-message success" title="Claim">
                            <span>Claim!</span>
                        </a>
                    }

                    {this.props.chat.assignedToUserId && this.props.chat.assignedToUserId == global.app.state.user.id &&
                        <a onClick={() => { console.log("Closed!"); this.closeChat(); }} className="btn btn-primary send-message danger" title="Close">
                            <span>Close</span>
                        </a>
                    
                    }

                    

                    <button className="btn btn-primary send-message" type="submit" disabled={this.props.disableSend || this.state.submitting || this.state.messageText.trim().length === 0} title={sendTip}>
                        <span>{sendLabel}</span>
                    </button>
                </div>
            </Form>

            {canDownload && <Form
				requestOpts={{
					blob: true
				}}
                action={"livesupportmessage/list.csv"}
                onValues={vals => {
                    var filter = {};
                    filter.where = {};
                    filter.where.LiveSupportChatId = this.props.chat.id;
					
					return filter;
                }}
				onSuccess={responseBlob => {
					var url = window.URL.createObjectURL(responseBlob);
					var a = document.createElement('a');
					a.href = url;
					a.download = "bridgestone-chat-history.csv";
					document.body.appendChild(a);
					a.click();
					a.remove();
				}}
            >
                <button type = "submit" className ="btn btn-primary send-message download-chat" onClick = {() => {console.log("Download!")}}>
                    <span>Download Chat</span>
                </button>
			</Form>
 }
        </div>;

    }

}
