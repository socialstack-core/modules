import Peer from 'UI/VideoChat/Peer';

export default class Peers extends React.Component {
	
	render(){
		var { huddleClient } = this.props;
		var peers = huddleClient.peers;
		var activeSpeakerId = huddleClient.room.activeSpeakerId;
		
		console.log(peers);
		
		return <div className="peers">
			{
				peers.map((peer) =>
				{
					return (
						<div
							className={'peer-container ' + ((peer.id === activeSpeakerId) ? 'active-speaker' : '')}
						>
							<Peer peer={peer} huddleClient={huddleClient} />
						</div>
					);
				})
			}
		</div>;
		
	}
	
}