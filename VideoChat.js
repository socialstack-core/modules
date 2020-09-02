import HuddleClient from 'UI/Functions/HuddleClient';
import Peers from 'UI/VideoChat/Peers';
import Me from 'UI/VideoChat/Me';

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
			test
		};

		this.onRoomUpdate = this.onRoomUpdate.bind(this);
	}

	onRoomUpdate(evt) {
		this.setState({ huddleClient: this.state.huddleClient });
	}

	componentDidMount() {
		const { huddleClient } = this.state;
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		if (!this.state.test) {
			huddleClient.join();
		}

	}

	componentWillUnmount() {
		const { huddleClient } = this.state;
		huddleClient.removeEventListener('roomupdate', this.onRoomUpdate);
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

		var videoClass = "videoChat";

		if (huddleClient && huddleClient.peers) {
			videoClass += " peers-" + huddleClient.peers.length;
		}

		return <div className={videoClass}>
			{/*<Notifications />*/}
			<div className='state'>
				<div className={'icon ' + room.state} title={stateDescription} />
			</div>

			{/* TODO: close button */}

			<Peers huddleClient={huddleClient} />
			<div className={'me-container ' + (amActiveSpeaker ? 'active-speaker' : '')}>
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