import Peer from 'UI/VideoChat/Peer';

export default class Peers extends React.Component {
	
	render(){
		var { huddleClient, activity, allowFullscreen } = this.props;
		var peers = huddleClient.peers || [];
		var activeSpeakerId = huddleClient.room.activeSpeakerId;
		
		peers = peers.filter(peer => !peer.device || !peer.device.huddleSpy);
		
		if(activity){
			// Push the activity so it has a proper index and the length etc works too:
			peers.push({_activity:activity, fullscreen: true});
		}
		
		if(!peers.length){
			return <div className="peers">
				<h2 className="nobody-else">
					Waiting for others to join the meeting
				</h2>
			</div>;
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
		
		var dataSharing = sharedPeers.join();
		var cappedPeerCount = peers.length > 8 ? 8 : peers.length;
		
		return <div className={"peers peers-" + cappedPeerCount} data-sharing={dataSharing} data-attendees={cappedPeerCount}>
			{
				peers.map((peer, index) =>
				{
					if(peer._activity){
						return <div data-peer={index} className="peer-container fullscreen peer-activity">
							{peer._activity}
						</div>
					}
					
					var btnClass = "btn peer-container";

					if (allowFullscreen) {
						btnClass += " allow-fullscreen";
					}

					if (peer.id === activeSpeakerId) {
						btnClass += " active-speaker";
					}

					if (peer.fullscreen && allowFullscreen) {
						btnClass += " fullscreen";
					}

					return (
						<button type="button" data-peer={index} className={btnClass} disabled={!allowFullscreen} title={allowFullscreen ? "Click to toggle expanded view" : ""}
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