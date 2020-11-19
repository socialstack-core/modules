import HuddleClient from 'UI/Functions/HuddleClient';
import Peers from 'UI/VideoChat/Peers';
import Me from 'UI/VideoChat/Me';
import Alert from 'UI/Alert';
import Container from 'UI/Container';

export default class VideoChat extends React.Component {

	constructor(props) {
		super(props);
		var test = false;

		var huddleClient = this.mount(props);

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
			currentX: 20,
			currentY: 70,
			meStyle: {
				bottom: '70px',
				left: '20px'
			}
		};
		
		this.onRoomUpdate = this.onRoomUpdate.bind(this);
		this.onEndMeContainerDrag = this.onEndMeContainerDrag.bind(this);
		this.onError = this.onError.bind(this);
	}
	
	mount(props){
		return new HuddleClient({
			roomId: (props.roomId || 1).toString(),
			produce: true,
			consume: true,
			useSimulcast: true,
			useSharingSimulcast: true
		});
	}
	
	componentWillReceiveProps(newProps){
		if(newProps.roomId != this.props.roomId){
			// Room change!
			this.state.huddleClient.close();
			var huddleClient = this.mount(newProps);
			this.setState({huddleClient});
			this.connect(huddleClient);
		}
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
		evt.stopPropagation();
		
		// get the mouse cursor position at startup:
		this.setState({
			drag:{
				startX: evt.clientX,
				startY: evt.clientY
			}
		});
		
		document.onmouseup = this.onEndMeContainerDrag;
	}

	onMoveMeContainerDrag(evt) {
		var {drag, meStyle, currentX, currentY} = this.state;
		
		if(!drag){
			return;
		}
		
		evt = evt || window.event;
		evt.preventDefault();
		evt.stopPropagation();
		
		var newX, newY;
		
		// calc new position
		var deltaX = evt.clientX - drag.startX;
		deltaY = drag.startY - evt.clientY;
		
		drag.startX = evt.clientX;
		drag.startY = evt.clientY;
		currentX += deltaX;
		currentY += deltaY;
		
		this.setState({
			drag,
			meStyle: {
				left: currentX + "px",
				bottom: currentY + "px"
			},
			currentX,
			currentY
		});
	}

	onEndMeContainerDrag() {
		document.onmouseup = null;
		this.setState({drag: null});
	}
	
	onRoomUpdate(evt) {
		this.props.onRoomUpdate && this.props.onRoomUpdate(evt, this.state.huddleClient);
		this.setState({ huddleClient: this.state.huddleClient, error: null });
	}
	
	connect(huddleClient){
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.addEventListener('error', this.onError);
		if (!this.state.test) {
			huddleClient.join();
		}
	}
	
	componentDidMount() {
		const { huddleClient } = this.state;
		this.connect(huddleClient);
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
			return <Container className="video-chat-error">
				<Alert type="error">
				{
					this.state.error.text || 
					'Unfortunately we ran into an issue connecting you to this meeting. Please check your internet connection and that you\'re invited to join this meeting.'
				}
				</Alert>
			</Container>;
		}
		
		var {children, allowFullscreen, onRenderPeer, onPreRender} = this.props;
		
		// If we have at least 1 actual child node, then there is a visible activity.
		// It acts like a fullscreen peer.
		var activity = children;
		
		var cfg = {
			className: 'videoChat',
			peersClassName: 'peers'
		};
		
		// Filter irrelevant peers:
		var peers = huddleClient.peers || [];
		peers = peers.filter(peer => !peer.device || !peer.device.huddleSpy);
		
		if(activity){
			// Push the activity so it has a proper index and the length etc works too:
			peers.push({_activity:activity, fullscreen: true});
		}
		
		// NB: using an array as we may potentially support multiple maximized videos in future
		var sharedPeers = [];
		
		if(activity){
			// it's always the last one, because we just pushed it in:
			sharedPeers.push(peers.length-1);
			allowFullscreen = false;
		}
		
		peers.map((peer, index) => {

			if (peer.fullscreen && allowFullscreen) {
				sharedPeers.push(index);
			}

		});
		
		onPreRender && onPreRender(cfg, peers, sharedPeers);
		
		return <div className={cfg.className} onMouseMove={e => this.onMoveMeContainerDrag(e)}>
			{/*<Notifications />*/}
			<div className='state'>
				<div className={'icon ' + room.state} title={stateDescription} />
			</div>

			{/* close button */}
			<a href="/" className="btn btn-close" title="Leave chat">
				<i className="fr fr-times"></i>
				<span className="sr-only">Leave chat</span>
			</a>
			<Peers 
				className={cfg.peersClassName}
				huddleClient={huddleClient}
				allowFullscreen={allowFullscreen}
				onRenderPeer={onRenderPeer}
				peers={peers}
				sharedPeers={sharedPeers}
				peerChange={() => {
					// Peer changed
					this.setState({});
				}}
			/>
			<div 
				className={'me-container ' + (amActiveSpeaker ? 'active-speaker' : '')}
				style={this.state.meStyle}
				onMouseDown={e => this.onStartMeContainerDrag(e)}
			>
				<Me huddleClient={huddleClient} />
			</div>


			{//These buttons turn off everyone else's video or audio (from your point of view)
			//Useful for personal bandwidth control, but unlikely that people will actually use them.
			}	
			<div className='sidebar'>
				{/*
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
				*/}
				
				{me.role != 1 &&  room.huddle && room.huddle.huddleType == 3 && <button 
					className = {'button raise-hand ' + (me.requestedToSpeak ? 'on' : 'off')} 
					title = "Raise hand to request sharing." 
					onClick = {() => {
						me.requestedToSpeak
							? huddleClient.requestToSpeak(false)
							: huddleClient.requestToSpeak(true);
					}}
				>	
					<i className="icon fas fa-hand-paper"/> 
				</button>}
			</div>
		</div>;

	}

}

VideoChat.propTypes = {
	roomId: 'int'
};