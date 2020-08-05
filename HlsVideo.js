import webRequest from 'UI/Functions/WebRequest';
import hlsjs from './hls.js';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import cache from 'UI/HlsVideo/Cache';


export default class HlsVideo extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
		this.onManifest = this.onManifest.bind(this);
		
		if(hlsjs.isSupported()) {
			var hls = this.state.hls = cache(this.getSource(props), this.onManifest);
		}
	}
	
	onManifest(){
		if(!this.video){
			return;
		}
		
		if(this.props.autoplay){
			this.video.pause();
			this.video.currentTime = 0;
			this.video.load();
			this.video.play();
		}
	}
	
	componentWillReceiveProps(props){
	}
	
	getSource(props){
		var {videoId, videoRef} = props;
		
		if(!videoId && videoRef){
			// extract id from ref:
			var refParts = getRef(videoRef, {url: true, dirs: ['video']}).split('-');
			return refParts[0] + '/manifest.m3u8';
		}
		
		return '/content/video/' + videoId + '/manifest.m3u8';
	}
	
	render(){
		return <div className="hlsVideo">
			<video {...omit(this.props, ['videoId', 'ref', 'autoplay'])} ref={video => {
				if(!video){
					return;
				}
				this.video = video;
				var hls = this.state.hls;
				
				if(hls){
					if(video != hls.media){
						hls.detachMedia();
						hls.attachMedia(video);
					}
					
				}else if (video.canPlayType('application/vnd.apple.mpegurl')) {
					
					// hls.js is not supported on platforms that do not have Media Source Extensions (MSE) enabled.
					// When the browser has built-in HLS support (check using `canPlayType`), we can provide an HLS manifest (i.e. .m3u8 URL) directly to the video element throught the `src` property.
					// This is using the built-in support of the plain video element, without using hls.js.
					// Note: it would be more normal to wait on the 'canplay' event below however on Safari (where you are most likely to find built-in HLS support) the video.src URL must be on the user-driven
					// white-list before a 'canplay' event will be emitted; the last video event that can be reliably listened-for when the URL is not on the white-list is 'loadedmetadata'.
					video.src = src;
					video.onloadedmetadata = this.onManifest;
				}
				
			}}/> 
		</div>;
		
	}
	
}

HlsVideo.propTypes = {
	videoId: 'int',
	autoplay: 'bool',
	muted: 'bool'
};