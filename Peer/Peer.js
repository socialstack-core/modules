import PeerView from 'UI/VideoChat/PeerView';

export default class Peer extends React.Component {
	
	render(){
		const {
			peer,
			huddleClient
		} = this.props;
		
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
		
		return (
			<div className="peer">
				<div className='indicators'>
					{!audioEnabled && (
						<div className='icon mic-off' />
					)}
					{!videoConsumer && (
						<div className='icon webcam-off' />
					)}
				</div>
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