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
	
	renderUser(user, subject, dateEdited, users) {
		return <div className="user-chat">
			{getRef(user.avatarRef, { size: 256 })}
			<div className="user-details">
				<h3 className="user-name">
					{user.firstName + ' ' + user.lastName}
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
		
		var filter = {
			where:this.props.filter,
			sort:{
				field: 'EditedUtc',
				direction: 'desc'
			}
		};
		
		return <div className="live-support-chats">
			<Loop raw paged={paginationSettings} live='LiveSupportChat' over={'livesupportchat/list'} filter={filter}>
				{
					chatChannel => {
						// Establish who we're chatting with.
						// The chat is with all of the given permitted users (one of whom is "me")
						var users = chatChannel.permittedUsers || [];
						users = users.map(permit => permit.permitted).filter(p => p!=null && !(p.type == "User" && p.id == user.id));
						
						if(!users.length){
							// This chat channel is with a deleted user, 
							// or anon (as is the case on coms from the homepage).
							users.push({
								type: 'User',
								firstName: 'Anonymous',
								lastName:''
							});
						}
						
						var chattingWith = users[0];
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
								{this.renderUser(chattingWith, chatChannel.subject, chatChannel.editedUtc, users)}
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