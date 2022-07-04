import webRequest from 'UI/Functions/WebRequest';
import dashjs from './dash.js';

export default class DashVideo extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
	}
	
	componentWillReceiveProps(props){
	}
	
	render(){
		return <div className="dashVideo">
			<video ref={vid => {
				this.videoRef = vid;
				
				var fileUrl = '/content/video/' + this.props.videoId + '/manifest.mpd';
				var player = global.dashjs.MediaPlayer().create();
				player.initialize(this.videoRef, fileUrl, true);
				
			}} muted={this.props.muted} autoplay={this.props.autoplay} controls /> 
		</div>;
		
	}
	
}

DashVideo.propTypes = {
	videoId: 'int',
	autoplay: 'bool',
	muted: 'bool'
};