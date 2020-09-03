import HuddleClient from 'UI/Functions/HuddleClient';
import Peers from 'UI/VideoChat/Peers';
import Me from 'UI/VideoChat/Me';
import Alert from 'UI/Alert';
import Container from 'UI/Container';

export default class VideoChat extends React.Component {

	constructor(props) {
		super(props);
		var test = false;

		var huddleClient = new HuddleClient({
			roomId: (props.roomId || 1).toString(),
			produce: true,
			consume: true,
			useSimulcast: true,
			useSharingSimulcast: true
		});

		if (typeof props.roomId === 'string' && props.roomId.length > 1) {
			if (props.roomId[0] == 't') {
				// tX where X is the number of peers in the meeting.
				// Ability to setup a test number of people in a meeting.
				test = true;

				huddleClient.peers = [];
				var pt = parseInt(props.roomId.substring(1));
				for (var i = 0; i < pt; i++) {
					huddleClient.peers.push({ test: true, consumers: [] });
				}

			}
		}

		this.state = {
			huddleClient,
			test,
			startX: 0,
			startY: 0
		};

		this.onRoomUpdate = this.onRoomUpdate.bind(this);
		this.onError = this.onError.bind(this);
		this.onStartMeContainerDrag = this.onStartMeContainerDrag.bind(this);
		this.onMoveMeContainerDrag = this.onMoveMeContainerDrag.bind(this);
	}
	
	onError(e){
		if(!e.minor){
			console.log(e);
			this.setState({
				error: e
			});
		}
	}

	onStartMeContainerDrag(evt) {
		evt = evt || window.event;
		evt.preventDefault();

		// get the mouse cursor position at startup:
		this.setState({
			startX: evt.clientX,
			startY: evt.clientY
		});

		document.onmouseup = this.onEndMeContainerDrag;
		document.onmousemove = this.onMoveMeContainerDrag;
	}

	onMoveMeContainerDrag(evt) {
		evt = evt || window.event;
		evt.preventDefault();

		var newX, newY;
		var meContainer = document.getElementById("me_container");

		if (!meContainer) {
			return;
		}

		// calc new position
		newX = this.state.startX - evt.clientX;
		newY = this.state.startY - evt.clientY;

		this.setState({
			startX: evt.clientX,
			startY: evt.clientY
		});

		// set new position
		var limitedX = Math.max(0, meContainer.offsetLeft - newX);
		limitedX = Math.min(limitedX, window.innerWidth - meContainer.offsetWidth);

		var limitedY = Math.min(window.innerHeight - meContainer.offsetHeight, window.innerHeight - meContainer.offsetTop - meContainer.offsetHeight + newY);
		limitedY = Math.max(limitedY, 0);

		meContainer.style.left = limitedX + "px";
		meContainer.style.bottom = limitedY + "px";
	}

	onEndMeContainerDrag() {
		document.onmouseup = null;
		document.onmousemove = null;
	}

	onRoomUpdate(evt) {
		this.setState({ huddleClient: this.state.huddleClient, error: null });
	}

	componentDidMount() {
		const { huddleClient } = this.state;
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.addEventListener('error', this.onError);
		if (!this.state.test) {
			huddleClient.join();
		}

		// make own video draggable
		var meContainer = document.getElementById("me_container");

		if (meContainer) {
			meContainer.onmousedown = this.onStartMeContainerDrag;
		}

	}

	componentWillUnmount() {
		const { huddleClient } = this.state;
		huddleClient.removeEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.removeEventListener('error', this.onError);
		if (!this.state.test) {
			huddleClient.close();
		}
	}

	render() {
		const {
			huddleClient
		} = this.state;

		//console.log("HC ", huddleClient);

		const {
			room,
			me
		} = huddleClient;

		var amActiveSpeaker = room.activeSpeakerId == me.id;

		var stateDescription = '';

		if (room && room.state) {

			switch (room.state) {
				case 'new':
					// ?
					stateDescription = '';
					break;

				case 'closed':
					stateDescription = 'Disconnected';
					break;

				case 'connected':
					stateDescription = 'Connected';
					break;
			}
		}
		
		if(this.state.error){
			return <Container>
				<Alert type="error">
					Unfortunately we ran into an issue connecting you to this meeting. Please check your internet connection and that you're invited to join this meeting.
				</Alert>
			</Container>;
		}
		
		var videoClass = "videoChat";

		if (huddleClient && huddleClient.peers) {
			videoClass += " peers-" + huddleClient.peers.length;
		}

		return <div className={videoClass}>
			{/*<Notifications />*/}
			<div className='state'>
				<div className={'icon ' + room.state} title={stateDescription} />
			</div>

			{/* close button */}
			<a href="/" className="btn btn-close" title="Leave chat">
				<i className="fr fr-times"></i>
				<span className="sr-only">Leave chat</span>
			</a>

			<Peers huddleClient={huddleClient} allowFullscreen={this.props.allowFullscreen} />
			<div id="me_container" className={'me-container ' + (amActiveSpeaker ? 'active-speaker' : '')}>
				<Me huddleClient={huddleClient} />
			</div>

			{/*
				These buttons turn off everyone else's video or audio (from your point of view)
				Useful for personal bandwidth control, but unlikely that people will actually use them.
				<div className='sidebar'>
					<div
						className={'button hide-videos ' + (me.audioOnly ? 'on' : 'off') + ' ' + (me.audioOnlyInProgress ? 'disabled' : '')}
						onClick={() =>
						{
							me.audioOnly ? huddleClient.disableAudioOnly() : huddleClient.enableAudioOnly();
						}}
					/>
					
					<div
						className={'button mute-audio ' + (me.audioMuted ? 'on' : 'off')}
						data-tip={'Mute/unmute participants\' audio'}
						onClick={() =>
						{
							me.audioMuted
								? huddleClient.unmuteAudio()
								: huddleClient.muteAudio();
						}}
					/>
				</div>
				*/}
		</div>;

	}

}

VideoChat.propTypes = {
	roomId: 'int'
};