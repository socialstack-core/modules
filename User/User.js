import getRef from 'UI/Functions/GetRef';
import { useState, useEffect, useRef } from 'react';
import Dropdown from 'UI/Dropdown';

const DEFAULT_RATIO = 16 / 9;

export default function User(props){
	var { user, isThumbnail, isDemo, node } = props;
	const userRef = useRef();
	
	var [videoPaused, setVideoPaused] = useState();
	var [videoCanPlay, setVideoCanPlay] = useState();
	var [sharingPaused, setSharingPaused] = useState();
	var [sharingCanPlay, setSharingCanPlay] = useState();
	var [ratio, setRatio] = useState();
	var [userStyle, setUserStyle] = useState();
	var [expandVideo, setExpandVideo] = useState(true);

	function updateAspectRatio(peer) {

		if (!peer) {
			return;
		}

		var rect = peer.getBoundingClientRect();
		var newRatio = (rect.width / rect.height).toFixed(1);

		if (ratio === newRatio) {
			return;
		}

		setUserStyle({ '--user-sharing-video-width': Math.ceil(Math.min(rect.width, rect.height) * .35) + 'px' });
		setRatio(newRatio);

		var newExpandVideo = newRatio < (1.6 * DEFAULT_RATIO);

		if (newExpandVideo != expandVideo) {
			setExpandVideo(newExpandVideo);
		}
	}
	
	function throttle(f, delay) {
		let timer = 0;
		return function (...args) {
			clearTimeout(timer);
			timer = setTimeout(() => f.apply(this, args), delay);
		}
	}

	// audio
	var audRef = React.useCallback(ele => {
		if(!ele){
			return;
		}
		
		var { user } = props;
		var audioTrack = user.audioTrack;
		
		if(audioTrack == ele.srcObject){
			return;
		}
		
		if (audioTrack) {
			console.log("Adding an audio track..", audioTrack);
			var stream = new MediaStream();
			stream.addTrack(audioTrack);
			ele.srcObject = stream;

			ele.play().catch((error) => console.warn('Failed to play audio: ', error));
		} else {
			ele.srcObject = null;
		}

	}, [props.user.audioTrack]);

	// video
	var vidRef = React.useCallback(ele => {
		if(!ele){
			return;
		}
		
		var { user } = props;
		var videoTrack = user.videoTrack;
		
		if(videoTrack == ele.srcObject){
			return;
		}
		
		if (videoTrack) {
			var stream = new MediaStream();
			stream.addTrack(videoTrack);

			ele.oncanplay = () => {
				// setVideoCanPlay(true)
			};

			ele.onplay = () => {
				// setVideoPaused(false);

				// Play audio too:
				// audRef.current.play().catch((error) => console.warn('Failed to play audio: ', error));
			};

			// ele.onpause = () => setVideoPaused(true);

			ele.srcObject = stream;
			
			console.log("Playing video!");
			ele.play().catch((error) => console.warn('Failed to play video: ', error));

			// this._startVideoResolution();
		} else {
			ele.srcObject = null;
		}
	
	}, [props.user.videoTrack]);
	
	// sharing
	var sharingRef = React.useCallback(ele => {
		if(!ele){
			return;
		}
		
		var { user } = props;
		var sharingTrack = user.sharingTrack;
		
		if(sharingTrack == ele.srcObject){
			return;
		}
		
		if (sharingTrack) {
			var stream = new MediaStream();
			stream.addTrack(sharingTrack);

			ele.oncanplay = () => {
				setSharingCanPlay(true)
			};

			ele.onplay = () => {
				setSharingPaused(false);
			};

			ele.onpause = () => setSharingPaused(true);

			ele.srcObject = stream;

			ele.play().catch((error) => console.warn('Failed to play sharing video: ', error));

			// this._startVideoResolution();
		} else {
			ele.srcObject = null;
		}

	}, [props.user.sharingTrack]);
	
	useEffect(() => {
		var peer = userRef.current;

		if (!peer || isThumbnail) {
			return;
		}

		var peerObserver;

		peerObserver = new ResizeObserver(throttle((entries) => {
			updateAspectRatio(peer);
		}, 500));

		peerObserver.observe(peer);
		updateAspectRatio(peer);

		return () => {
			peerObserver.unobserve(peer);
			peerObserver.disconnect();
		};

	});

	var userClass = "huddle-chat__user";
	var avatarClass = "huddle-chat__user-avatar";
	var audioOn = user.isMicrophoneOn;
	var videoOn = user.isWebcamOn;
	var sharingOn = user.isSharing;
	
	if (user.gone) {
		audioOn = false;
		videoOn = false;
		sharingOn = false;
		userClass += " huddle-chat__user--gone";
    }

	if (audioOn) {
		userClass += " huddle-chat__user--audio";
    }

	if (videoOn) {
		userClass += " huddle-chat__user--video";
		avatarClass += " huddle-chat__user-avatar--hidden";
	}

	if (user.creatorUser && user.creatorUser.avatarRef) {
		userClass += " huddle-chat__user--avatar";
	}

	if (!expandVideo) {
		userClass += " huddle-chat__user--contain-video";
    }

	if (isThumbnail) {
		userClass += " huddle-chat__user--thumbnail";
	}

	if (sharingOn && !isThumbnail) {
		userClass += " huddle-chat__user--sharing";
	}

	/* TODO: determine active speaker status
	if (isActive) {
		userClass += " huddle-chat__user--active";
	}
	*/

	var userName = user.creatorUser && user.creatorUser.username ? user.creatorUser.username : 'Unknown user';

	var videoStyle = videoOn ? {} : { 'display': 'none' };
	var audioStyle = audioOn ? undefined : { 'display': 'none' };
	var sharingStyle = sharingOn && !isThumbnail ? undefined : { 'display': 'none' };

	var Node = node ?? 'li';

	var labelJsx = <i className="fal fa-fw fa-ellipsis-h"></i>;

	return <Node className={userClass} ref={userRef} style={userStyle}>
		{/*
		<header className="huddle-chat__user-header">
			<button title={audioOn ? "Mute" : "Unmute"} type="button" disabled={user.audioDisabled ? "disabled" : undefined}
				className={audioOn ? "btn huddle-chat__user-audio huddle-chat__user--audio-on" : "btn huddle-chat__user--audio-off"}
				onClick={() => props.setAudio(audioOn ? 0 : 1)}>
				<i className={audioOn ? "fas fa-fw fa-microphone" : "fas fa-fw fa-microphone-slash"} />
			</button>
			<button title={videoOn ? "Hide video" : "Show video"} type="button" disabled={user.videoDisabled ? "disabled" : undefined}
				className={videoOn ? "btn huddle-chat__user-video huddle-chat__user--video-on" : "btn huddle-chat__user--video-off"}
				onClick={() => props.setVideo(videoOn ? 0 : 1)}>
				<i className={videoOn ? "fas fa-fw fa-video" : "fas fa-fw fa-video-slash"} />
			</button>
		</header>
		 */}

		{!videoOn && <>
			<div className={avatarClass}>
				{user.creatorUser && user.creatorUser.avatarRef && getRef(user.creatorUser.avatarRef, { size: 256, attribs: { alt: userName } })}
			</div>
		</>}

		<video className="huddle-chat__user-video" style={videoStyle}
			ref={vidRef}
			autoplay
			playsinline
			muted
			loop={isDemo ? true : undefined}
			controls={false}
		/>

		<audio className="huddle-chat__user-audio" style={audioStyle}
			ref={audRef}
			autoplay
			playsinline
			muted={false}
			controls={false}
		/>

		<video className="huddle-chat__user-share" style={sharingStyle}
			ref={sharingRef}
			autoplay
			playsinline
			muted
			loop={isDemo ? true : undefined}
			controls={false}
		/>

		<footer className="huddle-chat__user-footer">
			<span className="huddle-chat__user-name">
				{/* user.avatarRef && getRef(user.avatarRef, { size: 24 }) */}
				{/*ratio*/}
				{!user.gone && <>
					<span data-clamp="1">
						{userName}
					</span>
					{!audioOn && <i className="fas fa-microphone-slash huddle-chat__user-audio" />}
				</>}
				{user.gone && <>
					{userName} has left the meeting
				</>}
			</span>

			{/* NB: disabled until options are included */}
			{!user.gone && true && <>
				{/* options dropup button */}
				<Dropdown title={"Options"} className="huddle-chat__user-options" label={labelJsx} variant="link" position="top" align="right">
					<li>
						<button type="button" className="btn dropdown-item">
							<i className="fal fa-fw fa-star"></i> {`Example option`}
						</button>
					</li>
					{/* now handled automatically */}
					{/*videoOn && <>
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => setExpandVideo(false)}>
							<i className={!expandVideo ? "fal fa-fw fa-check" : "fal fa-fw"}></i> Scale video
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => setExpandVideo(true)}>
							<i className={expandVideo ? "fal fa-fw fa-check" : "fal fa-fw"}></i> Expand video
						</button>
					</li>
				</>*/}
					{/*
				<li>
					<hr class="dropdown-divider" />
				</li>
				<li>
					<button type="button" className="btn dropdown-item">
						<i className="fal fa-fw fa-hand-paper"></i> Raise hand
					</button>
				</li>
				*/}
				</Dropdown>
			</>}
		</footer>
	</Node>;
}