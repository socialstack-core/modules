import VideoChat from 'UI/VideoChat';
import ThreeDObject from 'UI/ThreeDObject';


export default class VideoChat3D extends React.Component {
	
	render(){
		// Just a wrapper around video chat but with specialised hooks into it.
		// Requires "placements" prop - an array of positions to put each peer at.
		// Note that beyond this length, or if a view is fullscreened or there's an activity etc, 
		// it will fallback to default layout provided by VideoChat.
		var placements = this.props.placements;
		
		return <VideoChat 
			{...this.props}
			onPreRender={(cfg, peers, sharedPeers) => {
				if((sharedPeers && sharedPeers.length) || !placements || peers.length>placements.length){
					return;
				}
				
				cfg.className="videoChat videoChat-3d";
				cfg.peersClassName="peers-3d";
			}}
			
			onRenderPeer={(peer, content, index, peers, sharedPeers, allowFullscreen) => {
				
				if((sharedPeers && sharedPeers.length) || !placements || peers.length>placements.length){
					return content;
				}
				
				var point = placements[index];
				
				return <ThreeDObject key={peer.id} position={point.position} scale={point.scale} rotation={point.rotation}>
					<div className="peer-3d">
						{content}
					</div>
				</ThreeDObject>;
				
			}}
		/>;
		
	}
	
}

VideoChat3D.propTypes = VideoChat.propTypes;
