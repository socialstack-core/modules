import Time from 'UI/Time';
import Loop from 'UI/Loop';
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
	
	render(){
		var { chat } = this.props;
		var { user } = global.app.state;
		
		return <div className="message-list">
			<div ref={r => this.history = r} className="message-history">
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
				}}
				
				groupAll
				>
					{all => {
						var msgs = all.map(pm => {
							
							// A message was made by creatorUser.
							// * pm.creatorUser is sender info
							var sender = pm.creatorUser;
							
							var fromThisSide = true;
							
							var messageClass = fromThisSide ? "message message-right" : "message";
							var dateClass = fromThisSide ? "message-date message-date-right" : "message-date";

							return <>
								<div className={messageClass}>
									{this.nl2br(pm.message)}
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
			<MessageCreate chat={chat} />
		</div>;
	}
	
}
