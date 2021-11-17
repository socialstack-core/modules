import webRequest, {lazyLoad} from 'UI/Functions/WebRequest';
import hlsjsRef from './static/hls.js';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';

// {Hls as hlsjs}

export default class HlsVideo extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
		this.onManifest = this.onManifest.bind(this);
		this.load(props);
	}
	
	onManifest(){
		if(!this.video){
			return;
		}
		
		var { hls }  = this.state;
		
		if(hls && hls.media != this.video){
			hls.detachMedia();
			hls.attachMedia(this.video);
		}
		
		if(this.props.autoplay){
			try{
				this.video.pause();
				this.video.currentTime = 0;
				this.video.load();
				var playPromise = this.video.play();
				
				if(playPromise && playPromise.then){
					playPromise.catch(e => {
						this.props.onAutoplayBlocked && this.props.onAutoplayBlocked();
					})
				}
			}catch(e){
				// autoplay block
				this.props.onAutoplayBlocked && this.props.onAutoplayBlocked();
			}
		}
	}
	
	componentWillUnmount(){
		this.clear();
	}
	
	clear(){
		if(this.state.hls){
			try{
				this.video.stop && this.video.stop();
				this.state.hls.stopLoad && this.state.hls.stopLoad();
				this.state.hls.destroy && this.state.hls.destroy();
			}catch(e){
				console.log('Error stopping HLS: ', e);
			}
		}
	}
	
	componentWillReceiveProps(props){
		if(props.videoId != this.props.videoId || props.videoRef != this.props.videoRef || this.props.url != props.url) {
			this.load(props);
		}
	}
	
	createPlayer(props, Hlsjs){
		var src = this.getSource(props);
		
		var hls = new Hlsjs({startFragPrefetch: true, liveMaxLatencyDuration: 12, liveSyncDuration: 3});
		hls.loadSource(src);
		hls.on(Hlsjs.Events.MANIFEST_PARSED, () => {
			hls.___manifest = true;	
			this.onManifest();
		});
		
		hls.on(Hlsjs.Events.SUBTITLE_TRACKS_UPDATED, () => {
			
			if(this.video && this.props.forcedCC){
				var subtitleTracks = this.video.textTracks;
				
				if(subtitleTracks){
					var fcc = this.props.forcedCC.trim().toLowerCase();
					for(var i=0;i<subtitleTracks.length;i++){
						var stTrack = subtitleTracks[i];
						if(stTrack.language.toLowerCase().trim() == fcc){
							stTrack.mode = 'showing';
						}else{
							stTrack.mode = 'disabled';
						}
					}
				}
			}
			
		});
		
		// When adding these, only the ones in the future and the latest from the past will be triggered.
		// Each entry is {time: serverTime, mtd: methodToTrigger}.
		// If it has an id field, it will ensure that it is ticked only ever once, even if you add it repeatedly.
		hls.addTimedEvents = events => {
			if(!events || !events.length){
				return;
			}
			
			if(!hls.__eTimer){
				hls.__pend = [];
				hls.__tickedEvts = {};
				
				// 10 times per second, check if there are any events in the queue.
				hls.__tick = () => {
					
					// Get time:
					var time = hls.getServerTime();
					
					if(time == 0){
						// Not loaded yet
						return;
					}
					
					// Find the latest one to activate.
					var liveNow = -1;
					for(var i=0; i<hls.__pend.length;i++){
						if(hls.__pend[i].time < time){
							liveNow = i;
						}else{
							break;
						}
					}
					
					if(liveNow != -1){
						
						var active = hls.__pend[liveNow];
						
						// Slice off that much:
						hls.__pend = hls.__pend.slice(liveNow + 1);
						
						// Trigger it:
						try{
							active.mtd && active.mtd();
						}catch(e){
							console.error(e);
						}
					}
					
				};
				hls.__eTimer = setInterval(hls.__tick, 100);
				
				hls.on(Hlsjs.Events.DESTROYING, () => {
					clearInterval(hls.__eTimer);
				});
			}
			
			// Add to pending:
			for(var i=events.length-1; i>=0;i--){
				var e = events[i];
				if(e.id && hls.__tickedEvts[e.id + "_"]){
					continue;
				}
				hls.__tickedEvts[e.id + "_"] = true;
				hls.__pend.push(e);
			}
			
			hls.__pend.sort((a,b) => a.time > b.time ? 1 : -1);
			hls.__tick();
		};
		
		/* a long integer in UTC milliseconds of the currently measurable time at the stream 
		server for the piece of the stream _this_ player is at */
		hls.getServerTime = () => {
			if(!hls.media || !hls.streamController){
				// Media not loaded
				return 0;
			}
			
			var fc = hls.streamController.fragCurrent;
			
			if(!fc){
				// Media not loaded
				return 0;
			}
			
			var serverTimestamp;
			
			if(fc == hls.__sChunk){
				// Cached:
				serverTimestamp = hls.__sStamp;
			}else{
				hls.__sChunk = fc;
				var fSlash = fc._url.lastIndexOf('/');
				
				// UTC in ms of the start of the current chunk:
				serverTimestamp = hls.__sStamp = parseInt(fc._url.substring(fSlash + 1, fc._url.length - 3));
			}
			
			// Current chunk starts in this many ms:
			var deltaInMs = (fc.start - hls.media.currentTime) * 1000;
			
			// which means the above timestamp is that delta in the future:
			return Math.floor(serverTimestamp - deltaInMs);
		};
		
		return hls;
	}
	
	load(props){
		lazyLoad(getRef(hlsjsRef, {url:1})).then(imported => {
			var Hls = imported.Hls;
			if(!Hls.isSupported()){
				this.setState({loaded: 1});
				return;
			}
			this.clear();
			var hls = this.createPlayer(props, Hls);
			this.setState({hls, loaded: 1});
			props.onPlayer && props.onPlayer(hls);
		});
	}
	
	getSource(props){
		var {videoRef, url, deprMode} = props;
		
		var result = url;
		
		if(!result){
			
			if(deprMode){
				// extract id from ref:
				var refParts = getRef(videoRef, {url: true, dirs: ['video']}).split('-');
				result = refParts[0] + '/manifest.m3u8';
			}else{
				result = getRef(videoRef, {url: true, size: 'chunks/manifest.m3u8'});
			}
			
		}
		
		if(result.indexOf('?') == -1){
			// Timestamp to avoid local caching:
			result += '?t=' + Date.now();
		}
		
		return result;
	}

	openFullscreen() {
		if(this.video) {
			if (this.video.requestFullscreen) {
				this.video.requestFullscreen();
			} else if (this.video.webkitRequestFullscreen) { /* Safari */
				this.video.webkitRequestFullscreen();
			} else if (this.video.msRequestFullscreen) { /* IE11 */
				this.video.msRequestFullscreen();
			}
		} else {
			console.log("failed fullscreen");
		}
	}
	
	render(){
		if(!this.state.loaded){
			return null;
		}
		
		if(this.props.fullScreen) {
			this.openFullscreen();
		}

		var className = this.props.className ? this.props.className + "-wrapper hlsVideo" : "hlsVideo";
		
		var poster = this.props.poster;
		
		if(poster === true){
			// Read it from the ref:
			poster = getRef(this.props.videoRef, {url: true, size: 'chunks/thumbnail.jpg'});
		}
		
		return <div className={className}>
			<video {...omit(this.props, ['videoId', 'videoRef', 'ref', 'autoplay'])} poster={poster} ref={video => {
				if(!video){
					return;
				}
				this.video = video;
				this.props.onVideo && this.props.onVideo(video);
				var hls = this.state.hls;
				
				if (!hls && video.canPlayType('application/vnd.apple.mpegurl')) {
					// hls.js is not supported on platforms that do not have Media Source Extensions (MSE) enabled.
					// When the browser has built-in HLS support (check using `canPlayType`), we can provide an HLS manifest (i.e. .m3u8 URL) directly to the video element throught the `src` property.
					// This is using the built-in support of the plain video element, without using hls.js.
					// Note: it would be more normal to wait on the 'canplay' event below however on Safari (where you are most likely to find built-in HLS support) the video.src URL must be on the user-driven
					// white-list before a 'canplay' event will be emitted; the last video event that can be reliably listened-for when the URL is not on the white-list is 'loadedmetadata'.
					video.src = this.getSource(this.props);
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