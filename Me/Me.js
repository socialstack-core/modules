import PeerView from 'UI/VideoChat/PeerView';

export default class Me extends React.Component {
	
	render(){
		const {
			huddleClient
		} = this.props;
		
		var user = global.app.state.user;
		
		var displayName = 'Anonymous';
		
		if(user && user.username){
			displayName = user.username;
		}
		
		var me = huddleClient.me;
		
		const connected = huddleClient.room.state === 'connected';
		
		const producersArray = huddleClient.producers;
		const audioProducer = producersArray.find((producer) => producer.track.kind === 'audio');
		const videoProducer = producersArray.find((producer) => producer.track.kind === 'video');
		var { room } = huddleClient;

		let micState;

		if (!me.canSendMic)
			micState = 'unsupported';
		else if (!audioProducer)
			micState = 'unsupported';
		else if (!audioProducer.paused)
			micState = 'on';
		else
			micState = 'off';

		let webcamState;

		if (!me.canSendWebcam)
			webcamState = 'unsupported';
		else if (videoProducer && videoProducer.type !== 'share')
			webcamState = 'on';
		else
			webcamState = 'off';

		let changeWebcamState;

		if (Boolean(videoProducer) && videoProducer.type !== 'share' && me.canChangeWebcam)
			changeWebcamState = 'on';
		else
			changeWebcamState = 'unsupported';

		let shareState;

		if (Boolean(videoProducer) && videoProducer.type === 'share')
			shareState = 'on';
		else
			shareState = 'off';

		const videoVisible = Boolean(videoProducer) && !videoProducer.paused;
		
		return (
			<div
				className="me"
				ref={(node) => (this._rootNode = node)}
			>
				{connected && (<>
					<div className='controls'>
						<div
							className={'button mic ' + micState}
							onClick={() =>
							{
								micState === 'on'
									? huddleClient.muteMic()
									: huddleClient.unmuteMic();
							}}
						/>

						<div
							className={'button webcam ' + webcamState + ' ' + ((me.webcamInProgress || me.shareInProgress) ? 'disabled': '')}
							onClick={() =>
							{
								if (webcamState === 'on')
								{
									huddleClient.disableWebcam();
								}
								else
								{
									huddleClient.enableWebcam();
								}
							}}
						/>

						<div
							className={'button change-webcam ' + changeWebcamState + ((me.webcamInProgress || me.shareInProgress) ? 'disabled' : '')}
							onClick={() => huddleClient.changeWebcam()}
						/>

						<div
							className={'button share ' + shareState + ((me.shareInProgress || me.webcamInProgress) ? 'disabled' : '')}
							onClick={() =>
							{
								if (shareState === 'on')
									huddleClient.disableShare();
								else
									huddleClient.enableShare();
							}}
						/>
					</div>
					{
						room.huddle && room.huddle.huddleType == 3 && (
							me.isPermittedSpeaker ? (
								<div className="live-indicator on">
									Live
								</div>
							) : (
								<div className="live-indicator off">
									Not Live
								</div>
							)
						)
					}
				</>)}
				
				<PeerView
					isMe
					peer={me}
					displayName={displayName}
					videoRtpParameters={videoProducer ? videoProducer.rtpParameters : null}
					audioTrack={audioProducer ? audioProducer.track : null}
					videoTrack={videoProducer ? videoProducer.track : null}
					videoVisible={videoVisible}
					audioScore={audioProducer ? audioProducer.score : null}
					videoScore={videoProducer ? videoProducer.score : null}
				/>
			</div>
		);
		
	}
	
}