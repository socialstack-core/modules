import {lazyLoad} from 'UI/Functions/WebRequest';
import hlsjsRef from './static/hls.js';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import Dropdown from 'UI/Dropdown';
import defaultPlayIcon from './play.svg';
import defaultPauseIcon from './pause.svg';
import defaultCcIcon from './cc.svg';
import defaultVolumeOnIcon from './volume-on.svg';
import defaultVolumeOffIcon from './volume-off.svg';
import defaultEnterFullscreenIcon from './enter-fullscreen.svg';
import defaultExitFullscreenIcon from './exit-fullscreen.svg';

// {Hls as hlsjs}

export default class HlsVideo extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {
			wrapperState: null,
			customControlsState: props.controlsBelow ? 'visible' : 'hidden',
			playState: 'hidden',
			volumeState: 'hidden',
			fullscreenState: 'off',
			fullscreenStyle: null,
			duration: 0,
			currentTime: 0,
			hasCaptions: false,
			captionIndex: -1,
			hasAudio: false,
			autoplayBlocked: false,
			showMuteWarning: true
		};
		this.onManifest = this.onManifest.bind(this);
		this.onAudioDetected = this.onAudioDetected.bind(this);
		this.toggleControls = this.toggleControls.bind(this);
		this.togglePlayState = this.togglePlayState.bind(this);
		this.toggleVolume = this.toggleVolume.bind(this);
		this.switchSubtitleTrack = this.switchSubtitleTrack.bind(this);
		this.dropdownWrapperRef = React.createRef();
		this.load(props);
	}

	// format seconds to HH:MM:SS
	formatTime(seconds) {
		var date = new Date(null);
		date.setSeconds(seconds);
		var iso = date.toISOString();

		// returns hh:mm:ss or mm:ss if less than an hour
		return seconds >= 3600 ? iso.substr(11, 8) : iso.substr(14, 5);
	}

    // toggle visibility of custom video controls
    toggleControls(visible) {

		if (!this.props.controlsBelow) {
			this.setState({customControlsState: visible ? "visible" : "hidden"});
		}

    }
	
    // toggle play button state (play/paused)
    togglePlayState() {

		if (!this.video) {
			return;
		}

		var videoState = this.video.paused || this.video.ended ? 'play' : 'pause';

		this.setState({
			wrapperState: videoState,
			playState: videoState
		});
    }

    // toggle volume button state (on/muted)
    toggleVolume() {

		if (!this.video) {
			return;
		}

		if (!this.video.muted) {
			this.setState({ showMuteWarning: false });
		}

		this.setState({ volumeState: this.video.muted ? 'off' : 'on' });
    }

	// switch subtitle track
	switchSubtitleTrack(index) {

		if (!this.video) {
			return;
		}

		// switch all tracks off
		for (var i = 0; i < this.video.textTracks.length; i++) {
			this.video.textTracks[i].default = false;
			this.video.textTracks[i].mode = 'disabled';
		}

		var selectedTrack = this.state.hls.subtitleTracks.filter(track => track.id == index);

		if (!selectedTrack || selectedTrack.length == 0) {
			this.setState({ captionIndex: -1 });
			return;
		}

		var lang = selectedTrack[0].lang.toLowerCase().trim();
		var videoTracks = this.video.textTracks;

		for (var i = 0; i < videoTracks.length; i++) {
			var track = videoTracks[i];

			if (track.language.toLowerCase().trim() == lang) {
				track.default = true;
				track.mode = 'showing';
			}
		}

	}

    // toggle fullscreen state
	/*
    toggleFullscreen() {
		
		if (isFullScreen()) {
			
			if (document.exitFullscreen) {
				document.exitFullscreen();
			} else if (document.mozCancelFullScreen) {
				document.mozCancelFullScreen();
			} else if (document.webkitCancelFullScreen) {
				document.webkitCancelFullScreen();
			} else if (document.msExitFullscreen) {
				document.msExitFullscreen();
			} else if (document.webkitExitFullscreen) {
				document.webkitExitFullscreen();
			}
			
		}
		else {

			if (!videoWrapperRef || !videoWrapperRef.current) {
				return;
			}
			
			if (videoWrapperRef.current.requestFullscreen) {
				videoWrapperRef.current.requestFullscreen();
			} else if (videoWrapperRef.current.mozRequestFullScreen) {
				videoWrapperRef.current.mozRequestFullScreen();
			} else if (videoWrapperRef.current.webkitRequestFullScreen) {
				videoWrapperRef.current.webkitRequestFullScreen(); 
			} else if (videoWrapperRef.current.msRequestFullscreen) {
				videoWrapperRef.current.msRequestFullscreen();
			}
			
			if (video && video.webkitEnterFullscreen) {
				video.webkitEnterFullscreen();
			}
			
		}

    }
	*/

    // is fullscreen mode available?
	/*
    var fullScreenEnabled = !!(document.fullscreenEnabled || 
        document.mozFullScreenEnabled || document.msFullscreenEnabled || 
        document.webkitSupportsFullscreen || document.webkitFullscreenEnabled || 
        document.createElement('video').webkitRequestFullScreen);
*/

	// check - in full screen mode?
    isFullScreen() {
       return !!(document.fullscreen || document.webkitIsFullScreen || document.mozFullScreen || document.msFullscreenElement || document.fullscreenElement);
    }

	onManifest(){

		if (!this.video){
			return;
		}

		var { hls }  = this.state;
		
		if (hls && hls.media != this.video){
			hls.detachMedia();
			hls.attachMedia(this.video);
		}

		if (this.video.muted) {
			this.setState({ volumeState: this.video.muted ? 'off' : 'on' });
		}

		if (this.props.autoplay) {
			try{
				this.video.pause();
				this.video.currentTime = 0;
				this.video.load();
				var playPromise = this.video.play();
				
				if (playPromise && playPromise.then){
					playPromise.catch(e => {
						this.setState({ autoplayBlocked: true });
						this.props.onAutoplayBlocked && this.props.onAutoplayBlocked();
					})
				}
			} catch(e) {
				// autoplay block
				this.setState({ autoplayBlocked: true });
				this.props.onAutoplayBlocked && this.props.onAutoplayBlocked();
			}
		}
	}

	onAudioDetected() {
		this.setState({ hasAudio: true });
	};

	componentWillUnmount(){
		this.clear();
	}
	
	clear(){
		if(this.state.hls && this.video){
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
		
		var hlsConfig = {startFragPrefetch: true, ...(props.hlsConfig || {})};
		
		var hls = new Hlsjs(hlsConfig);
		hls.loadSource(src);
		hls.on(Hlsjs.Events.MANIFEST_PARSED, () => {
			hls.___manifest = true;	
			this.onManifest();
		});
		
		hls.on(Hlsjs.Events.SUBTITLE_TRACKS_UPDATED, () => {
			this.setState({ hasCaptions: hls.subtitleTracks.length > 0 });

			if (!this.video) {
				return;
			}

			var subtitleTracks = this.video.textTracks;

			// disable all tracks by default
			for (var i = 0; i < subtitleTracks.length; i++) {
				subtitleTracks[i].default = false;
				subtitleTracks[i].mode = 'disabled';
			}

			if (this.props.forcedCC){
				
				if (subtitleTracks) {
					var fcc = this.props.forcedCC.trim().toLowerCase();

					for (var i = 0; i < subtitleTracks.length; i++) {
						var stTrack = subtitleTracks[i];

						if (stTrack.language.toLowerCase().trim() == fcc) {
							stTrack.default = true;
							stTrack.mode = 'showing';
						} else {
							stTrack.default = false;
							stTrack.mode = 'disabled';
						}
					}
				}
			}
			
		});
		
		hls.on(Hlsjs.Events.SUBTITLE_TRACK_SWITCH, (eventType, data) => {

			if (data?.id >= 0) {
				this.setState({ captionIndex: data.id });
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
				
				if(document.createElement('video').canPlayType('application/vnd.apple.mpegurl')){
					this.setState({nativeMode: true, loaded: 1});
				}else{
					this.setState({loaded: 1});
				}
				
				
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

		// always show controls below video (as opposed to overlaying on hover)?
		if (this.props.controlsBelow) {
			className += " hlsVideo--controls-below";
		}
		
		var poster = this.props.poster;
		
		if(poster === true){
			// Read it from the ref:
			poster = getRef(this.props.videoRef, {url: true, size: 'chunks/thumbnail.jpg'});
		}
		
		var showPlayPause = this.props.showPlayPause;
		var playIconImage = getRef(this.props.playIcon || defaultPlayIcon);
		var pauseIconImage = getRef(this.props.pauseIcon || defaultPauseIcon);

		var ccIconImage = getRef(this.props.ccIcon || defaultCcIcon);

		var volumeOnIconImage = getRef(this.props.volumeOnIcon || defaultVolumeOnIcon);
		var volumeOffIconImage = getRef(this.props.volumeOffIcon || defaultVolumeOffIcon);

		var enterFullscreenIconImage = getRef(this.props.enterFullscreenIcon || defaultEnterFullscreenIcon);
		var exitFullscreenIconImage = getRef(this.props.exitFullscreenIcon || defaultExitFullscreenIcon);

		var videoControlsClass = "video__controls";

		if (this.props.invert) {
			videoControlsClass += " video__controls--invert";
		}

		var volumeOffClass = "video__controls-volume--off";

		if (this.state.showMuteWarning) {
			volumeOffClass += " mute-warning";
		}
		
		var nativeMode = this.state.nativeMode;
		
		if(nativeMode){
			// hls.js is not supported on platforms that do not have Media Source Extensions (MSE) enabled.
			return <div className={className}>
				<video {...omit(this.props, ['videoId', 'videoRef', 'ref', 'autoplay', 'hlsconfig'])} poster={poster} onLoadedMetadata={this.onManifest} src={this.getSource(this.props)} controls />
			</div>;
		}
		
		return <div className={className}>
			<video {...omit(this.props, ['videoId', 'videoRef', 'ref', 'autoplay', 'hlsconfig'])} poster={poster} ref={video => {
				if(!video){
					return;
				}
				
				this.video = video;
				this.props.onVideo && this.props.onVideo(video);
				var hls = this.state.hls;
				
				if(!hls){
					return;
				}
				
				video.oncanplaythrough = this.onAudioDetected;
				
				/*
				
				if (!hls && video.canPlayType('application/vnd.apple.mpegurl')) {
					// hls.js is not supported on platforms that do not have Media Source Extensions (MSE) enabled.
					// When the browser has built-in HLS support (check using `canPlayType`), we can provide an HLS manifest (i.e. .m3u8 URL) directly to the video element throught the `src` property.
					// This is using the built-in support of the plain video element, without using hls.js.
					// Note: it would be more normal to wait on the 'canplay' event below however on Safari (where you are most likely to find built-in HLS support) the video.src URL must be on the user-driven
					// white-list before a 'canplay' event will be emitted; the last video event that can be reliably listened-for when the URL is not on the white-list is 'loadedmetadata'.
					video.src = this.getSource(this.props);
					video.onloadedmetadata = this.onManifest;
					
					return;
				}
				*/
				
				/*
                video.addEventListener("webkitendfullscreen", function(){
                    setFullscreenData(false);
                }, false);
				*/

                // update progress
				/*
                video.addEventListener('timeupdate', function() {
					setDuration(video.duration);
                    setCurrentTime(video.currentTime);
                });
				*/

                // always show controls for touch devices
				/*
                if (!window.matchMedia('(hover: hover)').matches) {
                    this.toggleControls(true);
                }
				*/
				
                // toggle play button icon based on state returned by video
                video.addEventListener('play', this.togglePlayState, false);
                
                video.addEventListener('pause', this.togglePlayState, false);
                
                // rewind video when finished
                video.addEventListener('ended', function() {

					if (!this.video) {
						return;
					}

					this.video.currentTime = 0;
                    this.setState({ currentTime: 0 });
                });

				/*
                // display fullscreen option if supported
                if (fullScreenEnabled) {
					setFullscreenStyle({ 'display': 'flex' });
                }
				*/

			}}/> 
			{!this.props.controls && <div className="video__controls-wrapper" data-state={this.state.customControlsState} 
				onMouseOver={() => { if (!this.props.controlsBelow) { this.toggleControls(true); }}} 
				onMouseOut={() => { if (!this.props.controlsBelow) { this.toggleControls(false); }}} 
				onClick={(e) => {

					if (e.target.classList.contains("dropdown-toggle") || this.dropdownWrapperRef?.current?.contains(e.target)) {
						return;
					}

					// toggle play/pause by clicking video
					if (showPlayPause || this.state.autoplayBlocked) {

						if (this.video.paused || this.video.ended) {
							this.video.play();
							
							if (this.props.disablepause) {
								this.setState({
									showPlayPause: false,
									autoplayBlocked: false
								});
							}
						} else {
							this.video.pause();
						}
					}
				}}>
				<div className={videoControlsClass}>
					{/* play / pause */}
					{(showPlayPause || this.state.autoplayBlocked) && <button className="btn video__controls-playtoggle" type="button" data-state={this.state.playState}
						onClick={(e) => {
							// click to toggle play/pause
							e.stopPropagation();

							if (this.video.paused || this.video.ended) {
								this.video.play();

								if (this.props.disablepause) {
									this.setState({
										showPlayPause: false,
										autoplayBlocked: false
									});
								}
							} else {
								this.video.pause();
							}

						}}>
						<span className="video__controls-playtoggle--play">
							{playIconImage}
						</span>
						<span className="video__controls-playtoggle--pause">
							{pauseIconImage}
						</span>
					</button>}

					{/* position */}
					{this.props.showPosition && <span className="video__controls-position">
						{this.formatTime(this.state.currentTime)}
					</span>}

					{/* progress bar */}
					{this.props.showProgress && <div className="video__controls-progress-wrapper">
						<progress className="video__controls-progress" value={this.state.currentTime} min="0" max={this.state.duration} 
							onClick={(e) => {
								e.stopPropagation();

								// allow click to skip
								if (showPlayPause || this.state.autoplayBlocked) {
									var rect = this.getBoundingClientRect();
									var pos = (e.pageX  - rect.left) / this.offsetWidth;
									this.video.currentTime = pos * this.video.duration;
									this.setState({ currentTime: this.video.currentTime });
								}

							}}
						/>
					</div>}

					{/* remaining */}
					{this.props.showRemaining && <span className="video__controls-remaining">
						{this.formatTime(this.state.duration - this.state.currentTime)}
					</span>}

					{/* mute */}
					{this.state.hasAudio && <button className="btn video__controls-volume" type="button" data-state={this.state.volumeState}
						onClick={(e) => {
							e.stopPropagation();
							this.video.muted = !this.video.muted;
							this.toggleVolume();
						}}>
						<span className="video__controls-volume--on">
							{volumeOnIconImage}
						</span>
						<span className={volumeOffClass}>
							{volumeOffIconImage}
						</span>
					</button>}

					{/* cc */}
					{this.video && this.state.hasCaptions && this.state.hls && <div className="video__controls-cc" ref={this.dropdownWrapperRef}>
						<Dropdown className="dropup" label={ccIconImage} variant="link">
							<li>
								<button type="button" className={this.state.captionIndex == -1 ? "btn dropdown-item active" : "btn dropdown-item"} onClick={() => this.switchSubtitleTrack(-1)}>
									Off
								</button>
							</li>
							{
								this.state.hls.subtitleTracks.map((track, index) => {
									return (
										<li key={track.id}>
											<button type="button" className={this.state.captionIndex == index ? "btn dropdown-item active" : "btn dropdown-item"} 
												onClick={() => this.switchSubtitleTrack(index)}>
												{track.name}
											</button>
										</li>
									);
								})
							}
						</Dropdown>
					</div>}

					{/* fullscreen */}
					{this.props.showFullscreen && <button className="btn video__controls-fullscreen" type="button" data-state={this.state.fullscreenState} style={this.state.fullscreenStyle}
						onClick={(e) => {
							e.stopPropagation();
							this.toggleFullscreen();
						}}>
						<span className="video__controls-fullscreen--on">
							{enterFullscreenIconImage}
						</span>
						<span className="video__controls-fullscreen--off">
							{exitFullscreenIconImage}
						</span>
					</button>}
				</div>
			</div>
			}
		</div>;
		
	}
	
}

HlsVideo.propTypes = {
	videoId: 'int',
	autoplay: 'bool',
	muted: 'bool'
};