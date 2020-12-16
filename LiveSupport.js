import Row from 'UI/Row';
import Col from 'UI/Column';
import ChatList from 'Admin/LiveSupport/ChatList';
import MessageList from 'UI/LiveSupport/MessageList';
import webRequest from 'UI/Functions/WebRequest';


export default class LiveSupport extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render() {
		var { user } = global.app.state;
		
		if(!user){
			return;
		}
		
		var landingPageClass = "live-chat-admin";
	
		if (this.state.chat) {
			landingPageClass += " chat-visible";	
		}
		
		return <div className={landingPageClass}>
			<Row>
				<Col className="messages-navigation" sizeXs={12} sizeSm={6} sizeMd={5}>
					<section className="meeting-chats">
						<h2 className="meeting-chats-title">
							Open support requests
						</h2>
						<ChatList showTime filter={{AssignedToUserId: null}} onClick={chat => {
							this.setState({
								meetingRequest: null,
								chat
							})
						}} />
						<h2 className="meeting-chats-title">
							Your requests
						</h2>
						<ChatList canClaim = {true} showTime filter={{AssignedToUserId: user.id}} onClick={chat => {
							this.setState({
								meetingRequest: null,
								chat
							})
						}} />
					</section>
				</Col>

				<Col className="messages-preview" sizeXs={12} sizeSm={6} sizeMd={7}>
					<div className="livesupport">
						{ this.state.chat && <MessageList canClaim = {true} chat={this.state.chat} /> }
					</div>
				</Col>
			</Row>
		</div>;
		
	}
	
}

LiveSupport.propTypes = {
};