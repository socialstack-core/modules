import Form from 'UI/Form';
import Input from 'UI/Input';
import Alert from 'UI/Alert';
import webRequest from 'UI/Functions/WebRequest';
import Calendar from 'UI/Calendar';

export default class Create extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            messageText: ""
        };
        //console.log("Message Create: ");
    }

    messageUpdated(e) {
        this.setState({
            messageText: e.target.value
        })
    }

    claimChat() {
        this.setState({submitting: true});
        webRequest("livesupportchat/" + this.props.chat.id, { assignedToUserId: global.app.state.user.id }).then(response => {
            this.props.onClaim && this.props.onClaim(response.json);
            this.setState({ claimed: response, submitting: false });
            console.log("Chat was claimed!");
        }).catch(error => {
            this.setState({submitting: false});
            console.log("There was an error claiming the chat.");
        });
    }

    closeChat() {
        this.setState({submitting: true});
        webRequest("livesupportchat/" + this.props.chat.id, { assignedToUserId: null, enteredQueueUtc: null}).then(response => {
            this.setState({submitting: false});
            console.log("Chat was closed!");
            this.props.onClose && this.props.onClose();
        }).catch(error => {
            this.setState({submitting: false});
            console.log("There was an error closing the chat.");
        });
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
        var useCalendarMessage = false;

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

            if (lastMessage.messageType == 8) {
                useCalendarMessage = true;
            }
        }

        return <div className="message-create">
            {useCalendarMessage ? <Calendar exclusionStartUtc = {"2021-01-10 00:00:00"} exclusionEndUtc = {"2021-01-18 00:00:00"} {...this.props}/> :
            <Form
                action="livesupportmessage"
                className="message-form"
                onSuccess={response => {
                    this.setState({ submitting: false, messageText: '' });
                    this.createTextarea && (this.createTextarea.value = '');
                }}
                onFailed={response => {
                    this.setState({ submitting: false, failure: response.message || 'Unable to send your message at the moment' });
                }}
                onValues={
                    values => {
                        //console.log("submitting!");
                        values.inReplyTo = this.props.replyTo;
                        this.setState({ submitting: true, failure: false });
                        values.liveSupportChatId = this.props.chat.id;
                        values.messageType = lastMessage.messageType;
                        return values;
                    }
                }
            >
                <div>
                    <Input 
                        onKeyPress={e => {
                            if (!this.state.submitting && this.state.messageText.trim().length > 0 && e.keyCode == 13) {
                                e.preventDefault();
                                e.target.form.submit();
                            }
                        }}
                        inputRef={e => {this.createTextarea = e}}
                        style={{ fontSize: "1em" }}
                        name="message"
                        type="textarea"
                        maxlength="1000"
                        autocorrect="off"
                        autocapitalize="off"
                        placeholder={placeholder}
                        label=""
                        noWrapper
                        validate={validate}
                        validateErrorLocation = "above"
                        onKeyUp={e => {
                            this.messageUpdated(e);
                        }}
                    />

                    {this.props.canClaim ? (
                        <a onClick={() => { !this.state.submitting && this.claimChat() }} disabled={this.state.submitting} className="btn btn-primary send-message success" title="Claim">
                            <span>Claim!</span>
                        </a>
                    ):(
                        this.props.chat.assignedToUserId && this.props.chat.assignedToUserId == global.app.state.user.id &&
                        <a onClick={() => { !this.state.submitting && this.closeChat() }} disabled={this.state.submitting} className="btn btn-primary send-message danger" title="Close">
                            <span>Close</span>
                        </a>
                    )}
                    <button className="btn btn-primary send-message" type="submit" disabled={this.props.disableSend || this.state.submitting || this.state.messageText.trim().length === 0} title={sendTip}>
                        <span>{sendLabel}</span>
                    </button>
                </div>
            </Form> }

            {canDownload && <Form
                action={"livesupportmessage/list"}
                onValues={vals => {
                    var filter = {};
                    filter.where = {};
                    filter.where.LiveSupportChatId = this.props.chat.id;
					
					return filter;
                }}
				onSuccess={response => {
                    // We need to piece together our response string using the responseBlob
                    //console.log(response);
                    var text = "";
                    response.results.forEach(message => {
                        //console.log(message.message);

                        // Let's build our final text object.
                        // Who made the message?
                        if(message.message && message.message != "") {
                            text += "["+message.createdUtc+"] ";
                            if(message.fromSupport) {
                                text += "Bridgestone Support: ";
                                text += message.message + "\n";
                            } else if (message.creatorUser.id != global.app.state.user.id) {
                                // Its from the user
                                text += message.creatorUser.firstName + ": ";
                                text += message.message + "\n";
                            } else {
                                // Its from the user
                                text += "You: ";
                                text += message.message + "\n";
                            }
                        }
                    });

                    //console.log(text);

					//var url = window.URL.createObjectURL(response);
					var a = document.createElement('a');
                    //a.href = url;
                    a.href = "data:text/plain/charset=utf-8," + encodeURIComponent(text);
					a.download = "bridgestone-chat-history.txt";
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
