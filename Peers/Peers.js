import Peer from 'UI/VideoChat/Peer';

export default class Peers extends React.Component {
	
	render(){
		var { huddleClient } = this.props;
		var peers = huddleClient.peers;
		var activeSpeakerId = huddleClient.room.activeSpeakerId;
		
		if(peers){
			peers = peers.filter(peer => !peer.device || !peer.device.huddleSpy);
		}
		
		if(!peers || !peers.length){
			return <div className="peers">
				<h2 className="nobody-else">
					Waiting for others to join the meeting
				</h2>
			</div>;
		}

		// NB: using an array as we may potentially support multiple maximized videos in future
		var sharedPeers = [];

		peers.map((peer, index) => {

			if (peer.fullscreen && this.props.allowFullscreen) {
				sharedPeers.push(index);
			}

		});

		var dataSharing = sharedPeers.join();

		if (props.forceThumbnails) {
			dataSharing = "forced";
		}

		return <div className="peers" data-sharing={dataSharing} data-attendees={peers ? peers.length : 0}>
			{
				peers.map((peer, index) =>
				{
					var btnClass = "btn peer-container";

					if (this.props.allowFullscreen) {
						btnClass += " allow-fullscreen";
					}

					if (peer.id === activeSpeakerId) {
						btnClass += " active-speaker";
					}

					if (peer.fullscreen && this.props.allowFullscreen) {
						btnClass += " fullscreen";
					}

					return (
						<button type="button" data-peer={index} className={btnClass} disabled={!this.props.allowFullscreen} title={this.props.allowFullscreen ? "Click to toggle expanded view" : ""}
							onClick={(e) => {

								// ensure we only have one fullscreen vid at once
								peers.map((otherPeer) => {

									if (otherPeer != peer) {
										otherPeer.fullscreen = false;
									}

								});
						
								peer.fullscreen = !peer.fullscreen;
								this.setState({});
						}}>
							<Peer peer={peer} huddleClient={huddleClient} />
						</button>
					);
				})
			}
		</div>;
		
	}
	
}