import Time from 'UI/Time';
import Loop from 'UI/Loop';
import Form from 'UI/Form';
import Canvas from 'UI/Canvas';
import MessageCreate from 'UI/LiveSupport/MessageCreate';
import getContentTypeId from 'UI/Functions/GetContentTypeId';

export default class MessageList extends React.Component {
	
	/*
	* Checks if two objects match by type and ID.
	* Works for users, companies etc.
	*/
	isMatch(a, b){
		// Content type and id match:
		return a != null && b != null && a.type == b.type && a.id == b.id;
	}
	
	nl2br (str) {
		if (typeof str === 'undefined' || str === null) {
			return '';
		}
		return str.split('\n').map((item, key) => <p>{item}</p>)
	}
	
	renderAttachment(json, message) {
		if(message.messageType == 1){
			// Input format with a surrounding form. This importantly blocks the regular free text box from appearing.
			// The rendered field(s) must supply a field called "message" with the selected value.
			return this.renderInput(json, message);
		}
		return <Canvas>{json}</Canvas>;
	}
	
	renderInput(json, message) {
		return <Form
			action = "livesupportmessage"
			onSuccess={response => {
				if(this.state.hideMessageBox == message.id){
					this.setState({hideMessageBox: false});
				}
			}}
			onFailed={response => {
				this.setState({submitting: false, failure: response.message || 'Unable to send your message at the moment'});
			}}
			onValues={
				values => {
					values.inReplyTo = message.replyTo;
					values.liveSupportChatId = this.props.chat.id;
					if(this.state.hideMessageBox == message.id){
						return values;
					}
				}
			}
		>
			<Canvas>{json}</Canvas>
		</Form>;
	}
	
	render(){
		var { chat, sendLabel, sendTip, placeholder } = this.props;
		var { user } = global.app.state;
		var { lastMessage } = this.state;
		
		return <div className="message-list">
			<div ref={r => this.history = r} className="message-history" data-simplebar data-simplebar-auto-hide="false">
				<Loop raw live='LiveSupportMessage' over='livesupportmessage/list' filter={{
					where: {
						LiveSupportChatId: chat.id
					},
					sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}
				}}
				reverse
				onLiveCreate={entity => {
					var e = this.history;
					setTimeout(() => {
						e.scrollTo(0, e.scrollHeight);
					}, 10);
					
					if(entity.messageType == 1){
						// requires special response from user. The extra payload is canvas JSON.
						this.setState({
							hideMessageBox: entity.id,
							lastMessage: entity

						});
					}
					
				}}
				
				groupAll
				>
					{all => {
						var last = all.length && all[all.length-1];
						if(this.state.lastMessage != last){
							setTimeout(() => {
								this.setState({lastMessage: last});
							}, 10);
						}

						var msgs = all.map(pm => {
							
							// A message was made by creatorUser.
							// * pm.creatorUser is sender info
							var sender = pm.creatorUser;
							
							var fromThisSide = (sender == null && user == null) || (user!= null && pm.userId == user.id);

							
							var messageClass = fromThisSide ? "message message-right" : "message";
							var dateClass = fromThisSide ? "message-date message-date-right" : "message-date";
							
							return <>
								<div className={messageClass}>
									{this.nl2br(pm.message)}
									{pm.payloadJson && <div className="message-canvas">
										{this.renderAttachment(pm.payloadJson, pm)} 
									</div>}
								</div>
								<div className={dateClass}>
									{sender ? sender.fullName : ''} <Time absolute compact withDate date={pm.createdUtc} />
								</div>
							</>;
						});
						
						return msgs;
					}}
				</Loop>
			</div>
			{(!lastMessage || lastMessage.messageType != 1) && <MessageCreate onClose = {this.props.onClose} lastMessage={lastMessage} replyTo={lastMessage ? lastMessage.replyTo : 0}  canClaim={this.props.canClaim} chat={chat} sendLabel={sendLabel} sendTip={sendTip} placeholder={placeholder} />}
		</div>;
	}
	
}
