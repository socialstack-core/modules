import Time from 'UI/Time';
import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import getRef from 'UI/Functions/GetRef';

export default class ChatList extends React.Component {

	constructor(props) {
		super(props);
	}

	handleHeaderToggle(replacementTitle) {
		// only do this for mobile
		// (effectively anything below iPad portrait res in this case)
		if (window.matchMedia("(max-width: 767px) and (pointer: coarse)").matches) {
			global.app.setState({
				searchableHeaderShowClose: true,
				searchableHeaderTitle: replacementTitle
			});
		}
	}
	
	renderUser(user, subject, dateEdited) {
		if(!user) {
			return;
		}

		return <div className="user-chat">
			{getRef(user.avatarRef, { size: 256 })}
			<div className="user-details">
				<h3 className="user-name">
					{ user.fullName || user.firstName + ' ' + user.lastName }
				</h3>
				{!this.props.meetingRequest && 
					<p className="subject-title">
						{subject}
					</p>
				}
			</div>
			{this.props.showTime && dateEdited &&
                <Time className="chat-date" absolute compact shortDay compactDayOnly withDate date={dateEdited} />
			}
		</div>;
	}
	
	render(){
		
		var { user } = global.app.state;
		
		if(!user){
			return;
		}
		
		var paginationSettings = {
			pageSize: 10,
			showInput: false,
			maxLinks: 4
		};
		
		/*
		var filter = {
			where:this.props.filter,
			sort:{
				field: 'EditedUtc',
				direction: 'desc'
			}
		};
		*/
		var filter = this.props.filter;
		
		return <div className="live-support-chats">
			<Loop raw paged={paginationSettings} live='LiveSupportChat' over={'livesupportchat/list'} filter={filter} orNone = {() => <h5>There are no active support requests right now.</h5>}>
				{
					chatChannel => {
						// Who we're chatting with:
						var chattingWith = chatChannel.creatorUser;
						if (chatChannel.fullName) {
							chattingWith.fullName = chatChannel.fullName;
						}

						return (
							<button type="button" className="chat-request" onClick={() => {
								var name = "Message";
								
								if (chattingWith.firstName && chattingWith.lastName) {
									name = chattingWith.firstName + ' ' + chattingWith.lastName;
								} else {
									if (chattingWith.name) {
										name = chattingWith.name;
									}
								}

								setTimeout(() => {
									// this.handleHeaderToggle(name);
									this.props.onClick && this.props.onClick(chatChannel);
								}, 10);
							}}>
								{this.renderUser(chattingWith, chatChannel.subject, chatChannel.editedUtc)}
							</button>
						);
						
					}
				}
			</Loop>
		</div>;
		
	}
	
}

ChatList.propTypes = {
	companyInbox: 'bool'
};