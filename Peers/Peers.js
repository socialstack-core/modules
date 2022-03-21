import Peer from 'UI/VideoChat/Peer';

const PEER_CAP = 10;

export default class Peers extends React.Component {
	
	render(){
		var { huddleClient, allowFullscreen, peers, sharedPeers, className } = this.props;
		var activeSpeakerId = huddleClient.room.activeSpeakerId;
		
		if(!peers.length){
			return <div className="peers">
				<h2 className="nobody-else">
					{this.props.holdingText || 'Waiting for others to join the meeting'}
				</h2>
			</div>;
		}
		
		var dataSharing = sharedPeers.join();
		var cappedPeerCount = peers.length > PEER_CAP ? PEER_CAP : peers.length;
		
		return <div className={className + " peers-" + cappedPeerCount} data-sharing={dataSharing} data-attendees={cappedPeerCount}>
			{
				peers.map((peer, index) =>
				{
					var result = null;
					
					if(peer._activity){
						result = <div key={peer.id} data-peer={index} className="peer-container fullscreen peer-activity">
							{peer._activity}
						</div>;
					}else{
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
		
						result = (
							<button key={peer.id} type="button" data-peer={index} className={btnClass} disabled={!allowFullscreen} title={allowFullscreen ? "Click to toggle expanded view" : ""}
								onClick={(e) => {
									// ensure we only have one fullscreen vid at once
									peers.map((otherPeer) => {

										if (otherPeer != peer) {
											otherPeer.fullscreen = false;
										}

									});
							
									peer.fullscreen = !peer.fullscreen;
									this.props.peerChange();
							}}>
								<Peer peer={peer} huddleClient={huddleClient} />
							</button>
						);
					}
					
					return this.props.onRenderPeer ? this.props.onRenderPeer(peer, result, index, peers, sharedPeers, allowFullscreen) : result;
				})
			}
		</div>;
		
	}
	
}