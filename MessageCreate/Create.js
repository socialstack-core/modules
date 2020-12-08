import Form from 'UI/Form';
import Input from 'UI/Input';
import Alert from 'UI/Alert';

export default class Create extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
			messageText: ""
        };
    }

    messageUpdated(e) {
        this.setState({
            messageText: e.target.value
        })
    }

	render(){

		return <div className="message-create">
			<Form
				action = "livesupportmessage"
				onSuccess={response => {
					this.setState({submitting: false, messageText: ''});
					this.createTextarea.value='';
				}}
				onFailed={response => {
					this.setState({submitting: false, failure: response.message || 'Unable to send your message at the moment'});
				}}
				onValues={
					values => {
						this.setState({submitting: true, failure: false});
						values.liveSupportChatId = this.props.chat.id;
						return values;
					}
				}
				className="message-form"
			>
        <div>
          <Input onKeyPress={e => {
              if(!this.state.submitting && this.state.messageText.trim().length > 0 && e.keyCode == 13){
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
            placeholder="Write your message here..."
            label=""
            noWrapper
            onKeyUp={e => {
              this.messageUpdated(e);
          }} />
          {this.state.failure && <Alert type="error">{this.state.failure}</Alert>}
          <button className="btn btn-primary send-message" type="submit" disabled={this.state.submitting || this.state.messageText.trim().length === 0} title="Send message">
            <span>Send</span>
          </button>
        </div>
			</Form>
		</div>;

	}

}
