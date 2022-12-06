import getRef from 'UI/Functions/GetRef';
import { useState, useEffect, useRef } from 'react';
import Dropdown from 'UI/Dropdown';
import HuddleEnums from 'UI/HuddleClient/HuddleEnums';

const DEFAULT_RATIO = 16 / 9;

export default function User(props){
	var {
		user, isThumbnail, node, huddleClient, showDebugInfo,
		isStage, isAudience, isPinned, isListView
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
	
	// video
	var vidRef = React.useCallback(ele => {
		if(!ele){
			return;
		}
		
		var { user } = props;
		var videoTrack = user.videoTrack;
		
		if (videoTrack) {
			if(videoTrack.isMediaSource){
				var src = window.URL.createObjectURL(videoTrack);
				ele.src = src;
				
				// Handling falling behind (including background pauses for power preseveration) - make sure the time is no more than 200ms
				ele.ontimeupdate = (event) => {
					
					var dur = videoTrack.currentMaxStreamTime;
					var time = ele.currentTime;
					
					if(time + 0.5 < dur){
						ele.currentTime = dur - 0.1;
						
						// ele.play().catch((error) => console.warn('Failed to play video: ', error));
					}
				};
			}else{
				var stream = new MediaStream();
				stream.addTrack(videoTrack);
				ele.srcObject = stream;
				
				console.log("Playing video!");
				ele.play().catch((error) => console.warn('Failed to play video: ', error));
			}
			
			/*
			ele.oncanplay = () => {
				// setVideoCanPlay(true)
			};

			ele.onplay = () => {
				// setVideoPaused(false);

				// Play audio too:
				// audRef.current.play().catch((error) => console.warn('Failed to play audio: ', error));
			};
			*/
			
			// ele.onpause = () => setVideoPaused(true);
			
			// this._startVideoResolution();
		} else {
			if(ele.src){
				ele.src = '';
				ele.ontimeupdate = null;
			}else{
				ele.srcObject = null;
			}
		}
		
	}, [props.user.videoTrack]);
	
	// sharing
	var sharingRef = React.useCallback(ele => {
		if(!ele){
			return;
		}
		
		var { user } = props;
		var sharingTrack = user.sharingTrack;
		
		if (sharingTrack) {
			
			ele.oncanplay = () => {
				setSharingCanPlay(true)
			};

			ele.onplay = () => {
				setSharingPaused(false);
			};

			ele.onpause = () => setSharingPaused(true);
			
			if(sharingTrack.isMediaSource){
				var src = window.URL.createObjectURL(sharingTrack);
				ele.src = src;
				
				// Handling falling behind (including background pauses for power preseveration) - make sure the time is no more than 200ms
				ele.ontimeupdate = (event) => {
					
					var dur = sharingTrack.currentMaxStreamTime;
					var time = ele.currentTime;
					
					if(time + 0.5 < dur){
						ele.currentTime = dur - 0.1;
						
						ele.play().catch((error) => console.warn('Failed to play video: ', error));
					}
				};
				
			}else{
				var stream = new MediaStream();
				stream.addTrack(sharingTrack);
				ele.srcObject = stream;
				
				ele.play().catch((error) => console.warn('Failed to play sharing video: ', error));
			}
			
			// this._startVideoResolution();
		} else {
			if(ele.src){
				ele.src = '';
				ele.ontimeupdate = null;
			}else{
				ele.srcObject = null;
			}
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
	var micBlocked = (user.blockedChannels & 1) == 1;
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

	if (videoOn && !isListView) {
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

	if (isListView) {
		userClass.push('huddle-chat__user--list-view');
	}

	/* TODO: determine active speaker status
	if (isActive) {
		userClass.push("huddle-chat__user--active");
	}
	*/

	var userName = user.creatorUser && user.creatorUser.username ? user.creatorUser.username : 'Unknown user';

	var videoStyle = videoOn ? {} : { 'display': 'none' };
	var sharingStyle = sharingOn && !isThumbnail ? undefined : { 'display': 'none' };

	var Node = node ?? 'li';

	var labelJsx = <i className="fal fa-fw fa-ellipsis-h"></i>;

	var isSelf = user.id == huddleClient.selfId;
	
	var userMenuJsx = (
		!user.gone && huddleClient.isHost() && !isSelf && <>
			{/* options dropup button */}
			<Dropdown title={"Options"} className="huddle-chat__user-options" label={labelJsx} variant="link" position={isThumbnail ? "bottom" : "top"} align="right">
				{isStage && <li>
					<button type="button" className="btn dropdown-item" onClick={() => {
						huddleClient.moveToAudience(user.id);
					}}>
						<i className="fal fa-fw fa-user-minus"></i> {`Remove from stage`}
					</button>
				</li>}

				{isAudience && <li>
					{user.isOnStage && <>
						<button type="button" className="btn dropdown-item" onClick={() => {
							huddleClient.moveToAudience(user.id);
						}}>
							<i className="fal fa-fw fa-user-minus"></i> {`Remove from stage`}
						</button>
					</>}

					{!user.isOnStage && <>
						<button type="button" className="btn dropdown-item" onClick={() => {
							huddleClient.moveToStage(user.id);
						}}>
							<i className="fal fa-fw fa-user-plus"></i> {`Add to stage`}
						</button>
					</>}
				</li>}

				{isPinned && <li>
					<button type="button" className="btn dropdown-item" onClick={() => {
						huddleClient.moveToAudience(user.id);
					}}>
						<i className="fal fa-fw fa-user-minus"></i> {`Remove from stage`}
					</button>
				</li>}

				<li>
					<hr class="dropdown-divider" />
				</li>
			
				{!micBlocked && audioOn && <li>
					<button type="button" className="btn dropdown-item" onClick={() => {
						var channels = user.channels;
						
						// 1 is the mic, 2 is webcam, 4 for screenshare.
						if(audioOn){
							channels &= ~1;
						}else{
							channels |= 1;
						}
						
						huddleClient.setRemoteChannels(user.id, channels);
					}}>
						<i className={audioOn ? "fas fa-fw fa-microphone-slash" : "fas fa-fw fa-microphone"}></i> {`Mute user`}
					</button>
				</li>}
				
				<li>
					<button type="button" className="btn dropdown-item" onClick={() => {
						var channels = user.blockedChannels;
						
						// Channel 1 for the mic, 2 for webcam, 4 for screenshare.
						if(micBlocked){
							channels &= ~1;
						}else{
							channels |= 1;
						}
						
						huddleClient.setBlockedChannels(user.id, channels);
					}}>
						<i className={micBlocked ? "fas fa-fw fa-microphone-slash" : "fas fa-fw fa-microphone"}></i> {micBlocked ? `Allow microphone` : `Block microphone`}
					</button>
				</li>
				
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
						<i className="fas fa-fw fa-ban"></i> {`Remove participant`}
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
		</>
		);

	var userMessage = '';

	if (user.gone) {
		userMessage = `has left the meeting`;
	} else {

		if (user.isSharing) {
			userMessage = `is sharing their screen`;
        }

    }

	return <Node className={userClass.join(' ')} ref={userRef} style={userStyle}>

		{ isListView ? 			
			
			<>				
				
				{ user.creatorUser.avatarRef ? (					
					<div className="huddle-chat__user-avatar">					
						{user.creatorUser && user.creatorUser.avatarRef && getRef(user.creatorUser.avatarRef, { size: 64, attribs: { alt: userName }, hideOnError: true })}
						{isHost && <span className="huddle-chat__user-ishost">
							{`Host`}
						</span>}
					</div>					
				) : (					
					<div className="huddle-chat__user-avatar huddle-chat__user-avatar--fallback">		
						<i className="fas fa-fw fa-user"></i>
					</div>					
				)}				
				
				<span className='huddle-chat__user-name-wrapper'>
					<span className="huddle-chat__user-name" data-clamp="1">{userName}</span>
					<span className="huddle-chat__user-message">{userMessage}</span>
				</span>

				{!user.gone && <span class="huddle-chat__user-actions">
					{userMenuJsx}
					{audioOn ? <i className="fas fa-fw fa-microphone huddle-chat__user-audio" /> : <i className="fas fa-fw fa-microphone-slash huddle-chat__user-audio" />}
					{isStage && <i className="fas fa-fw fa-star huddle-chat__user-stage-icon" />}
				</span>}
				
			</>
			
			:

			<>

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
			
			<video className="huddle-chat__user-share" style={sharingStyle}
				ref={sharingRef}
				autoplay
				playsinline
				muted
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

				{userMenuJsx}

			</footer>
			
			</>

		}

	</Node>;
}