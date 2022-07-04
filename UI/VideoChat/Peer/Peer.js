import PeerView from 'UI/VideoChat/PeerView';

export default class Peer extends React.Component {

	setSpeaker(state, e){
		const {
			peer,
			huddleClient
		} = this.props;
		
		huddleClient.setAsSpeaker(peer, state);
		e.stopPropagation();
		e.stopImmediatePropagation && e.stopImmediatePropagation();
		e.preventDefault();
	}
	
	render(){
		const {
			allowFullscreen,
			disablePeerControls,
			peer,
			huddleClient
		} = this.props;
		
		var { room } = huddleClient;
		
		const audioConsumer = peer.consumers.find((consumer) => consumer.track.kind === 'audio');
		const videoConsumer = peer.consumers.find((consumer) => consumer.track.kind === 'video');
		
		var audioMuted = huddleClient.me.audioMuted;
		
		const audioEnabled = (
			Boolean(audioConsumer) &&
			!audioConsumer.locallyPaused &&
			!audioConsumer.remotelyPaused
		);

		const videoVisible = (
			Boolean(videoConsumer) &&
			!videoConsumer.locallyPaused &&
			!videoConsumer.remotelyPaused
		);

		var audioAvailable = Boolean(audioConsumer);
		var videoAvailable = Boolean(videoConsumer);

		return (
			<div className="peer">
				<div className='indicators'>
					{disablePeerControls ?
						allowFullscreen &&
							<div className="peer__indicator">
								<i className="fa fa-expand-arrows-alt"></i>
							</div>
					:
						<>
							<button type="button" className="peer__indicator peer__indicator--audio" disabled={!audioAvailable ? true : undefined}
								onClick={(e) => {
									(audioEnabled
										? huddleClient.muteMic()
										: huddleClient.unmuteMic()).then(() => this.setState({}));
								}}>
								<span className={"icon mic-" + (audioEnabled ? "on" : "off") }></span>
							</button>

							<button type="button" className="peer__indicator peer__indicator--video" disabled={!videoAvailable ? true : undefined}
								onClick={() => {
									(videoVisible
										? huddleClient.disableWebcam()
										: huddleClient.enableWebcam()).then(() => this.setState({}));
								}}>
								<span className={"icon webcam-" + (videoVisible ? "on" : "off") }></span>
							</button>
						</>
					}

					{// If we are an admin, we should see a button to make this user a permitted speaker if they are not.
					huddleClient.me.huddleRole == 1 && room.huddle && room.huddle.huddleType == 3 &&(peer.profile.isPermittedSpeaker ? <button className = "btn btn-danger" onClick = {e => {
						this.setSpeaker(false, e);
					}}>
						Revoke Speaker
					</button> : <button className = "btn btn-success" onClick = {e => {
						this.setSpeaker(true, e);
					}}>
						Permit Speaker
					</button>)}
				</div>
				{peer.profile.requestedToSpeak && <div className = "raised-hand">
					<i class="fas fa-hand-paper"></i>
				</div>}
				<PeerView
					peer={peer}
					videoRtpParameters={videoConsumer ? videoConsumer.rtpParameters : null}
					consumerSpatialLayers={videoConsumer ? videoConsumer.spatialLayers : null}
					consumerTemporalLayers={videoConsumer ? videoConsumer.temporalLayers : null}
					consumerCurrentSpatialLayer={
						videoConsumer ? videoConsumer.currentSpatialLayer : null
					}
					audioTrack={audioConsumer ? audioConsumer.track : null}
					videoTrack={videoConsumer ? videoConsumer.track : null}
					audioMuted={audioMuted}
					videoVisible={videoVisible}
					videoMultiLayer={videoConsumer && videoConsumer.type !== 'simple'}
					videoScore={videoConsumer ? videoConsumer.score : null}
				/>
			</div>
		);
	}
	
}