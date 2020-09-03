import Peer from 'UI/VideoChat/Peer';

export default class Peers extends React.Component {
	
	render(){
		var { huddleClient } = this.props;
		var peers = huddleClient.peers;
		var activeSpeakerId = huddleClient.room.activeSpeakerId;
		
		if(!peers || !peers.length){
			return <div className="peers">
				<h2 className="nobody-else">
					Waiting for others to join the meeting
				</h2>
			</div>;
		}
		
		return <div className="peers">
			{
				peers.map((peer) =>
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
						<button type="button" className={btnClass} disabled={!this.props.allowFullscreen} title={this.props.allowFullscreen ? "Click to toggle expanded view" : ""}
							onClick={(e) => {
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