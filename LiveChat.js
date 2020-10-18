import Loop from 'UI/Loop';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import getRef from 'UI/Functions/GetRef';
import headerInform from 'UI/Functions/HeaderInform';
import dateTools from 'UI/Functions/DateTools';
import webRequest from 'UI/Functions/WebRequest';
import getContentTypeId from 'UI/Functions/GetContentTypeId';

var chatMsgType = getContentTypeId('LiveChatMessage');

export default class LiveChat extends React.Component {

	constructor(props) {
		super(props);
		this.state = {
			messageText: "",
			flags: {}
		};
	}

	messageUpdated(e) {
		this.setState({
			messageText: e.target.value
		})
	}
	
	isModerator(user){
		return user.role == 1 || user.role == 2 || user.role == 7;
	}

	scrollChatToBottom() {

		setTimeout(() => {
			if (!this.chatRef) {
				return;
			}
			this.chatRef.scrollTo(0, this.chatRef.scrollHeight + 200);
		}, 100);

	}
	
	isFlagged(id){
		return !!this.state.flags['' + id];
	}
	
	moderate(chatMessage) {
		const { user } = global.app.state;
		
		if(!user){
			return;
		}
		
		if(user.id == chatMessage.userId){
			return;
		}
		
		var { flags } = this.state;
		
		if(user.role == 1 || user.role == 2 || user.role == 7){
			// Moderator - option to insta delete in a modal:
			this.setState({
				moderating: chatMessage
			});
		}else if(!this.isFlagged(chatMessage.id)){
			// Flag it:
			flags['' + chatMessage.id] = true;
			this.setState({flags});
			
			webRequest('userflag', {
				contentId: chatMessage.id,
				contentTypeId: chatMsgType
			}).then(response => {
				flags['' + chatMessage.id] = response.json.id;
				headerInform("Message flagged");
			})
		}else{
			// Delete flag:
			webRequest('userflag/' + this.state.flags['' + chatMessage.id], null, {method: 'DELETE'}).then(response => {
				delete flags['' + chatMessage.id];
			})
		}
	}
	
	render(){
		const { videoId } = this.props;
		const { user } = global.app.state;

		return <div className="liveChat" style={{ background: 'white' }}>
			<div ref={e => this.chatRef = e} className="chat">
				<Loop
					over={"livechatmessage/list"}
					live={"LiveChatMessage"}
					filter={{where:{VideoId: videoId}, pageSize: 50, sort: {
						field: 'CreatedUtc',
						direction: 'desc'
					}}}
					reverse
					onLiveCreate={entity => {
						setTimeout(() => {
							this.scrollChatToBottom();
						}, 100);
					}}
					onResults ={res => {
						setTimeout(() => {
							this.scrollChatToBottom();
						}, 100);
						return res;
					}}
				>
					{chatMessage => {
						
						var { creatorUser } = chatMessage;
						
						if(!creatorUser){
							return;
						}
						
						var flagged;
						if(chatMessage.profanityWeight || chatMessage.userFlags>=3){
							// If I wrote it, or this user is a moderator/ admin, it still displays, but with an indicator.
							flagged = true;
							
							// Either the person who wrote it, or a mod:
							if(!user || creatorUser.id != user.id && !this.isModerator(user)){
								return;
							}
						}
						
						if(this.isFlagged(chatMessage.id)){
							flagged = true;
						}

						var creatorName = 'Anonymous';

						if (creatorUser) {
							creatorName = creatorUser.firstName + ' ' + creatorUser.lastName;
						}

						var chatDate = dateTools.isoConvert(chatMessage.editedUtc);
						var chatDateTime = chatDate.getDate().toString().padStart(2, '0') + '/' +
							(chatDate.getMonth() + 1).toString().padStart(2, '0') + '/' +
							chatDate.getFullYear() + ' @ ' +
							chatDate.getHours() + ':' + chatDate.getMinutes().toString().padStart(2, '0');

						var title = creatorName + ' (' + chatDateTime + ')';

						return (<div className="message">
							{(!creatorUser || !creatorUser.avatarRef) && (
								<i className="fr fr-user no-avatar" title={title}></i>
							)}
							{creatorUser && creatorUser.avatarRef && 
								<a href={'/user/' + creatorUser.id}>
									{getRef(creatorUser.avatarRef, { size: 32, attribs: { alt: creatorName, title: title }})}
								</a>
							}
							<span className="text">
								{chatMessage.message}
							</span>
							{creatorUser && user && creatorUser.id != user.id && (
								<span className="controls">
									<button type="button" className="btn btn-link chat-to-user" title={"Send a message to " + creatorName} onClick={() => {
										global.app.setState({
											startChat: {
												onSuccess: () => {
													global.app.setState({ startChat: false });
													headerInform("Message sent");
												},
												recipient: creatorUser
											}
										});
									}}>
										<i className="fr fr-paper-plane"></i>
										<span className="sr-only">Direct message</span>
									</button>
									<button type="button" onClick={() =>{
											this.moderate(chatMessage)
										}} className="btn btn-link moderation-status" title={flagged ? 'Waiting for moderation' : 'Flag this comment'}>
										<i className={flagged ? "fr fr-flag-alt" : "fr fr-flag"}></i>
										<span className="sr-only">
											Moderation status
										</span>
									</button>
								</span>
							)}
						</div>);
					}}
				</Loop>
			</div>
			<div className="input">
				<Form action="livechatmessage"
					onSuccess={response => {
						this.setState({ submitting: false, messageText:'' });
						this.msgBox.value = '';
						this.scrollChatToBottom();
					}}
					onFailed={response => {
						this.setState({ submitting: false });
					}}
					onValues={
						v => {
							this.setState({ submitting: true });
							v.VideoId = videoId;
							return v;
						}
					}
				>
					<Input onKeyPress={e => {
						if(!this.state.submitting && this.state.messageText.trim().length > 0 && e.keyCode == 13){
							e.preventDefault();
							e.target.form.submit();
						}
					}} inputRef={e => this.msgBox = e} name="message" type="textarea" maxlength="200" autocorrect="off" autocapitalize="off"
						placeholder="Type your comment ..." onKeyUp={e => {
							this.messageUpdated(e);
						}} />
					<button type="submit" className="btn btn-link" disabled={this.state.submitting || this.state.messageText.trim().length === 0} title="Send message">
						<i className="fr fr-paper-plane"></i>
						<span className="sr-only">Send message</span>
					</button>
				</Form>
			</div>
			<Modal visible={!!this.state.moderating} onClose={() => {
				this.setState({
					moderating: false
				})
			}}>
				{this.renderModModal(this.state.moderating)}
			</Modal>
		</div>;
	}
	
	deleteMessage(msg){
		this.setState({deleting: true});
		webRequest('livechatmessage/' + msg.id, null, {method: 'DELETE'}).then(() => {
			this.setState({deleting: false, moderating: false});
		});
	}
	
	renderModModal(msg){
		if(!msg){
			return;
		}
		
		return <div>
			<p>
				Delete this message?
			</p>
			
			<div className="modal-internal-footer">
				<button disabled={this.state.deleting} className="btn btn-secondary btn-sm" onClick={() => {
					this.setState({
						moderating: false
					})
				}}>
					No
				</button>

				<button disabled={this.state.deleting} className="btn btn-primary btn-sm" onClick={() => {
					this.deleteMessage(msg);
				}}>
					Yes
				</button>
			</div>

		</div>;
	}
}