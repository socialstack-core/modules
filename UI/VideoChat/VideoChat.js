import HuddleClient from 'UI/Functions/HuddleClient';
import Peers from 'UI/VideoChat/Peers';
import Me from 'UI/VideoChat/Me';
import Alert from 'UI/Alert';
import Container from 'UI/Container';
import Row from 'UI/Row';
import { SessionConsumer } from 'UI/Session';
import CallUI from 'UI/VideoChat/CallUI';

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
				huddleClient.test = true;
				var pt = parseInt(props.roomId.substring(1));
				for (var i = 0; i < pt; i++) {
					huddleClient.peers.push({
						test: true,
						consumers: [],
						device: {},
						profile: props.testProfile || {
							isPermittedSpeaker: true,
							displayName: 'Test Device'
						}
					});
				}

			}
		}

		this.state = {
			huddleClient,
			test,
			currentX: 20,
			currentY: 70,
			meStyle: {
				bottom: '0px',
				left: '50px'
			},
			ringing: this.props.ringing
		};

		this.onRoomUpdate = this.onRoomUpdate.bind(this);
		this.onEndMeContainerDrag = this.onEndMeContainerDrag.bind(this);
		this.onError = this.onError.bind(this);
		this.onPeerAdd = this.onPeerAdd.bind(this);
		this.onPeerRemove = this.onPeerRemove.bind(this);
	}

	mount(props) {
		return new HuddleClient({
			roomSlug: props.roomSlug,
			meetingUrl: props.meetingUrl, // Can use this + huddle prop instead of roomSlug or roomId
			huddle: props.huddle,
			roomId: (props.roomId || 1).toString(),
			produce: props.initialProduce === undefined ? true : props.initialProduce,
			consume: props.initialConsume === undefined ? true : props.initialConsume,
			useSimulcast: true,
			useSharingSimulcast: true,
			directChatOnly: props.directChatOnly,
			excludeRoles: props.excludeRoles,
			cameraQuality: props.cameraQuality
		});
	}

	componentWillReceiveProps(newProps) {
		if (newProps.roomId != this.props.roomId) {
			// Room change!
			this.state.huddleClient.close();
			var huddleClient = this.mount(newProps);
			this.setState({ huddleClient });
			this.connect(huddleClient);
		}

		// did we receive a new list of user that need to be rung?
		if (newProps.ringing != this.props.ringing) {
			this.setState({ ringing: newProps.ringing });
		}
	}

	componentDidUpdate(prevProps) {
		this.updateRingingList();
	}

	// Used to update the ringing mechanism and UI's
	updateRingingList() {
		var { ringing } = this.state;
		var updated = false; // This will toggle to true if there is a need to update the ringing list.

		// There is a chance that a user we are ringing may have connected, let's verify.
		if (this.state.huddleClient.peers && this.state.huddleClient.peers.length && ringing && ringing.length) {
			this.state.huddleClient.peers.forEach((peer) => {
				// Let's iterate our currently ringing users - is this user any of them?
				ringing.forEach((ringer, index) => {

					if(ringer.id == peer.profile.userId || ringer.id == this.state.huddleClient.me.profile.userId) {
						// We have a hit, let's remove this user from the ringing list.
						ringing.splice(index, 1);
						updated = true;
					}
				});
			});
		}

		if (updated) {
			this.setState({ ringing });
		}
	}

	onError(e) {
		if (!e.minor) {
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
			drag: {
				startX: evt.clientX,
				startY: evt.clientY
			}
		});

		document.onmouseup = this.onEndMeContainerDrag;
	}

	onMoveMeContainerDrag(evt) {
		var { drag, meStyle, currentX, currentY } = this.state;

		if (!drag) {
			return;
		}

		evt = evt || window.event;
		evt.preventDefault();
		evt.stopPropagation();

		var newX, newY;

		// calc new position
		var deltaX = evt.clientX - drag.startX;
		var deltaY = drag.startY - evt.clientY;

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
		this.setState({ drag: null });
	}

	onRoomUpdate(evt) {
		this.props.onRoomUpdate && this.props.onRoomUpdate(evt, this.state.huddleClient);
		this.setState({ huddleClient: this.state.huddleClient, error: null });
	}

	onPeerAdd(evt) {
		// Peer changed
		this.setState({});
	}

	onPeerRemove(evt) {
		// Peer changed
		this.setState({});

		if (this.props.closeWhenNoPeers && this.state.huddleClient && this.state.huddleClient.peers && this.state.huddleClient.peers.length == 0) {
			this.props.onClose && this.props.onClose();
		}
	}

	connect(huddleClient) {
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.addEventListener('error', this.onError);
		huddleClient.addEventListener('peeradd', this.onPeerAdd);
		huddleClient.addEventListener('peerremove', this.onPeerRemove);
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
		huddleClient.removeEventListener('peeradd', this.onPeerAdd);
		huddleClient.removeEventListener('peerremove', this.onPeerRemove);

		if (!this.state.test) {
			huddleClient.close();
		}

		this.props.clearRinging && this.props.clearRinging();
	}

	render() {
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>;
	}

	renderIntl(session) {
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

		if (this.state.error) {
			return <Container className="video-chat-error">
				<Alert type="error">
					{
						this.state.error.text ||
						'Unfortunately we ran into an issue connecting you to this meeting. Please check your internet connection and that you\'re invited to join this meeting.'
					}
				</Alert>
			</Container>;
		}

		var { children, allowFullscreen, disablePeerControls, onRenderPeer, onPreRender, onClose, closeButtonText } = this.props;

		// If we have at least 1 actual child node, then there is a visible activity.
		// It acts like a fullscreen peer.
		var activity = children;

		var cfg = {
			className: 'videoChat',
			peersClassName: 'peers'
		};

		// Filter irrelevant peers:
		var peers = huddleClient.peers || [];
		var videoPeers = [];
		var raisedHands = [];

		peers.forEach(peer => {
			if (!peer.device || peer.device.huddleSpy) {
				return;
			}

			if (peer.profile.isPermittedSpeaker) {
				videoPeers.push(peer);
			}

			if (peer.profile && peer.profile.requestedToSpeak) {
				raisedHands.push(peer);
			}

		});

		peers = videoPeers;

		if (activity) {
			// Push the activity so it has a proper index and the length etc works too:
			peers.push({ _activity: activity, fullscreen: true });
		}

		// NB: using an array as we may potentially support multiple maximized videos in future
		var sharedPeers = [];

		if (activity) {
			// it's always the last one, because we just pushed it in:
			sharedPeers.push(peers.length - 1);
			allowFullscreen = false;
		}

		peers.map((peer, index) => {

			if (peer.fullscreen && allowFullscreen) {
				sharedPeers.push(index);
			}

		});

		onPreRender && onPreRender(cfg, peers, sharedPeers);

		var closeButtonHref = "/";

		if (onClose) {
			closeButtonHref = "#";
		}

		return <div className={cfg.className} onMouseMove={e => this.onMoveMeContainerDrag(e)}>
			{/*<Notifications />*/}
			<div className='state'>
				<div className={'icon ' + room.state} title={stateDescription} />
			</div>

			{/* close button */}
			<a href={closeButtonHref} className="btn btn-close" title={closeButtonText} onClick={onClose ? () => onClose() : undefined}>
				<i className="fr fr-times"></i>
				<span className="sr-only">{closeButtonText}</span>
			</a>
			<Peers
				className={cfg.peersClassName}
				huddleClient={huddleClient}
				allowFullscreen={allowFullscreen}
				disablePeerControls={disablePeerControls}
				onRenderPeer={onRenderPeer}
				peers={peers}
				holdingText={this.props.holdingText}
				sharedPeers={sharedPeers}
				peerChange={() => {
					// Peer changed
					this.setState({});
				}}
			/>

			{!this.props.hideMeView && this.props.floatMeView && (this.props.initialProduce === undefined || this.props.initialProduce || (!this.props.initialProduce && me.profile && me.profile.forceStartProducing)) &&
				<div
					className={'me-container ' + (amActiveSpeaker ? 'active-speaker' : '')}
					style={this.state.meStyle}
					onMouseDown={e => this.onStartMeContainerDrag(e)}
				>
					<Me huddleClient={huddleClient} session={session} />
				</div>
			}
			{this.props.showRaisedHands &&
				<div className="raised-hands">
					{/*<Row>
					<i class="fas fa-hand-paper icon"></i> Luke Briggs
				</Row>
				<Row>
					<i class="fas fa-volume icon live"></i> Michael Rogers
				</Row>
				<Row>
					<i class="fas fa-circle icon live"></i> John Safeway
				</Row>*/}
					{raisedHands.map(personWithRaisedHand => {

						var isLive = personWithRaisedHand.profile.isPermittedSpeaker;
						var name = personWithRaisedHand.profile.displayName;

						return <Row>
							{personWithRaisedHand.profile.directChatIds ? <i class="fal fa-volume icon"></i> : <i class="fas fa-hand-paper icon"></i>}
							{this.props.isDirector ? <a href="#" onClick={() => {
								huddleClient.updatePeer(personWithRaisedHand, {
									directChatIds: [session.user.id]
								});
							}}>
								{name}
							</a> : name
							}

							{this.props.isDirector &&
								<a href="#" onClick={() => {
									huddleClient.updatePeer(personWithRaisedHand, {
										requestedToSpeak: false
									});
								}}>
									<i class="fas fa-times-circle icon" style={{ color: "red" }}></i>
								</a>
							}
						</Row>

					})}
				</div>
			}

			{this.state.ringing && this.state.ringing.length && <CallUI users={this.state.ringing} onClose={() => {
				this.setState({ ringing: [] });
			}} />}

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

				{me.profile.huddleRole != 1 && room.huddle && (room.huddle.huddleType == 3 || room.huddle.huddleType == 4) && <button
					className={'button raise-hand ' + (me.profile.requestedToSpeak ? 'on' : 'off')}
					title="Raise hand to request sharing."
					onClick={() => {
						me.profile.requestedToSpeak
							? huddleClient.requestToSpeak(false)
							: huddleClient.requestToSpeak(true);
					}}
				>
					<i className="icon fas fa-hand-paper" />
				</button>}
			</div>
			{me.profile.directChatIds && me.profile.directChatIds.length && (<span className="producer"><i class="fal fa-volume"></i> Speaking with Producer</span>)}
		</div>;
	}
}

VideoChat.defaultProps = {
	intialProduce: true,
	intialConsume: true,
	showRaisedHands: true,
	hideMeView: false,
	floatMeView: true,
	closeWhenNoPeers: false,
	closeButtonText: 'Leave chat'
};

VideoChat.propTypes = {
	roomId: 'int',
	roomSlug: 'string',
	hideMeView: 'bool',
	floatMeView: 'bool',
	closeWhenNoPeers: 'bool',
	closeButtonText: 'string'
};