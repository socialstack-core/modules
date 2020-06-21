import HuddleClient from 'UI/Functions/HuddleClient';
import Peers from 'UI/VideoChat/Peers';
import Me from 'UI/VideoChat/Me';

export default class VideoChat extends React.Component {
	
	constructor(props){
		super(props);
		
		var huddleClient = new HuddleClient({
			roomId: (this.props.roomId || 1).toString(),
			produce: true,
			consume: true,
			useSimulcast: true,
			useSharingSimulcast: true
		});
		
		this.state = {
			huddleClient
		};
		
		this.onRoomUpdate = this.onRoomUpdate.bind(this);
	}
	
	onRoomUpdate(evt)
	{
		this.setState({huddleClient: this.state.huddleClient});
	}
	
	componentDidMount()
	{
		const { huddleClient } = this.state;
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.join();
		
	}
	
	componentWillUnmount()
	{
		const { huddleClient } = this.state;
		huddleClient.removeEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.close();
	}
	
	render(){
		const {
			huddleClient
		} = this.state;
		
		const {
			room,
			me
		} = huddleClient;
		
		var amActiveSpeaker = room.activeSpeakerId == me.id;
		
		return <div className="videoChat">
				{/*<Notifications />*/}
				<div className='state'>
					<div className={'icon ' + room.state} />
				</div>
				<Peers huddleClient={huddleClient}/>
				<div
					className={'me-container ' + (amActiveSpeaker ? 'active-speaker' : '')}
				>
					<Me huddleClient={huddleClient}/>
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
	
};