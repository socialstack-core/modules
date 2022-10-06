import getRef from 'UI/Functions/GetRef';
import { useState, useEffect, useRef } from 'react';
import Dropdown from 'UI/Dropdown';
import HuddleEnums from 'UI/HuddleClient/HuddleEnums';

const DEFAULT_RATIO = 16 / 9;

export default function User(props){
	var {
		user, isThumbnail, node, huddleClient, showDebugInfo,
		isStage, isAudience, isPinned
	} = props;
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

	var userClass = ["huddle-chat__user"];
	var avatarClass = ["huddle-chat__user-avatar"];
	var audioOn = user.isMicrophoneOn;
	var videoOn = user.isWebcamOn;
	var sharingOn = user.isSharing;
	
	if (user.gone) {
		audioOn = false;
		videoOn = false;
		sharingOn = false;
		userClass.push("huddle-chat__user--gone");
    }

	if (audioOn) {
		userClass.push("huddle-chat__user--audio");
    }

	if (videoOn) {
		userClass.push("huddle-chat__user--video");
		avatarClass.push("huddle-chat__user-avatar--hidden");
	}

	if (user.creatorUser && user.creatorUser.avatarRef) {
		userClass.push("huddle-chat__user--avatar");
	}

	if (!expandVideo) {
		userClass.push("huddle-chat__user--contain-video");
    }

	if (isThumbnail) {
		userClass.push("huddle-chat__user--thumbnail");
	}

	if (sharingOn && !isThumbnail) {
		userClass.push("huddle-chat__user--sharing");
	}

	// if not a guest role
	var isHost = HuddleEnums.isHost(user.role);

	if (isHost) {
		userClass.push('huddle-chat__user--host');
	}

	/* TODO: determine active speaker status
	if (isActive) {
		userClass.push("huddle-chat__user--active");
	}
	*/

	var userName = user.creatorUser && user.creatorUser.username ? user.creatorUser.username : 'Unknown user';

	var videoStyle = videoOn ? {} : { 'display': 'none' };
	var audioStyle = audioOn ? undefined : { 'display': 'none' };
	var sharingStyle = sharingOn && !isThumbnail ? undefined : { 'display': 'none' };

	var Node = node ?? 'li';

	var labelJsx = <i className="fal fa-fw fa-ellipsis-h"></i>;

	var usernameClass = ['huddle-chat__user-name'];

	return <Node className={userClass.join(' ')} ref={userRef} style={userStyle}>
		<header className="huddle-chat__user-header">
			{isHost && <>
				<span className="huddle-chat__user-host badge bg-primary">
					{`Host`}
				</span>
			</>}
			{showDebugInfo && <>
				<span className="huddle-chat__user-host badge bg-secondary">
					{user.id}
				</span>
			</>}
			{/*
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
				*/}
		</header>

		{!videoOn && <>
			<div className={avatarClass.join(' ')}>
				{user.creatorUser && user.creatorUser.avatarRef && getRef(user.creatorUser.avatarRef, { size: 256, attribs: { alt: userName }, hideOnError: true })}
			</div>
		</>}

		<video className="huddle-chat__user-video" style={videoStyle}
			ref={vidRef}
			autoplay
			playsinline
			muted
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
			controls={false}
		/>

		<footer className="huddle-chat__user-footer">
			<span className={usernameClass.join(' ')}>
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

			{!user.gone && huddleClient.isHost() && <>
				{/* options dropup button */}
				<Dropdown title={"Options"} className="huddle-chat__user-options" label={labelJsx} variant="link" position="top" align="right">
					{isStage && <li>
						<button type="button" className="btn dropdown-item" onClick={() => {
							huddleClient.moveToAudience(user.id);
						}}>
							<i className="fal fa-fw fa-users"></i> {`Join audience`}
						</button>
					</li>}

					{isAudience && <li>
						<button type="button" className="btn dropdown-item" onClick={() => {
							huddleClient.moveToStage(user.id);
						}}>
							<i className="fal fa-fw fa-walking"></i> {`Move to stage`}
						</button>
					</li>}

					{isPinned && <li>
						<button type="button" className="btn dropdown-item" onClick={() => {
							huddleClient.moveToAudience(user.id);
						}}>
							<i className="fal fa-fw fa-users"></i> {`Join audience`}
						</button>
					</li>}

					<li>
						<hr class="dropdown-divider" />
					</li>

					{/* TODO: disable if user hasn't selected an audio device */}
					{/*
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => { }}>
							<i className={audioOn ? "fas fa-fw fa-microphone-slash" : "fas fa-fw fa-microphone"}></i> {audioOn ? `Mute user` : `Unmute user`}
						</button>
					</li>
					*/}

					{/* TODO: disable if user hasn't selected a video device */}
					{/*
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => { }}>
							<i className={videoOn ? "fas fa-fw fa-video-slash" : "fas fa-fw fa-video"}></i> {videoOn ? `Disable user video` : `Enable user video`}
						</button>
					</li>

					{sharingOn && <>
						<li>
							<button type="button" className="btn dropdown-item" onClick={() => { }}>
								<i className="fas fa-fw fa-video-slash"></i> {`Stop user sharing`}
							</button>
						</li>
					</>}
					*/}

					<li>
						<button type="button" className="btn dropdown-item dropdown-item--danger" onClick={() => { huddleClient.kick(user.id) }}>
							<i className="fas fa-fw fa-ban"></i> {`Kick user`}
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