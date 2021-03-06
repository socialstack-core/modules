import Time from 'UI/Time';
import Loop from 'UI/Loop';
import getRef from 'UI/Functions/GetRef';
import {SessionConsumer} from 'UI/Session';

export default class ChatList extends React.Component {

	constructor(props) {
		super(props);
	}

	handleHeaderToggle(replacementTitle) {
		// only do this for mobile
		// (effectively anything below iPad portrait res in this case)
		/*if (window.matchMedia("(max-width: 767px) and (pointer: coarse)").matches) {
			global.app.setState({
				searchableHeaderShowClose: true,
				searchableHeaderTitle: replacementTitle
			});
		}*/
	}
	
	renderUser(user, subject, dateEdited) {
		if(!user) {
			return;
		}

		return <div className="user-chat">
			{getRef(user.avatarRef, { size: 100 })}
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
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}

	renderIntl(session){
		
		var { user } = session;
		
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
                        // TODO: fix the cause of this error properly
                        if (chattingWith == null) {
                            chattingWith = {};
                        }  
						if (chatChannel.fullName) {
							chattingWith.fullName = chatChannel.fullName;
						}

						/*
						var name = "Message";
						if (chattingWith.firstName && chattingWith.lastName) {
							name = chattingWith.firstName + ' ' + chattingWith.lastName;
						} else {
							if (chattingWith.name) {
								name = chattingWith.name;
							}
						}
						*/
						
						return (
							<button type="button" className="chat-request" onClick={() => {
								
								
								

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