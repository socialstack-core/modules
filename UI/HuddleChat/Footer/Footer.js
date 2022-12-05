import Dropdown from 'UI/Dropdown';
import Modal from 'UI/Modal';
import Alert from 'UI/Alert';
import Playback from 'UI/HuddleChat/Playback';
import { useState, useEffect, useRef } from 'react';
import firefoxPermissions from './sharing/firefox_permissions.png';
import safariPreferences from './sharing/safari_preferences.png';
import safariWebsites from './sharing/safari_websites.png';
import getRef from 'UI/Functions/GetRef';

const SHARE_CANCELLED = 999;

export default function Footer(props) {
	var { huddleClient, audioOn, videoOn, shareOn, isHost, playbackInfo, disableRecording, recordMode } = props;
	const [recordConfirmShown, setRecordConfirmShown] = useState(false);
	const [screenshareCancelledShown, setScreenshareCancelledShown] = useState(false);
	const [firefox, setFirefox] = useState(false);
	const [safari, setSafari] = useState(false);
	const firefoxRef = useRef(null);
	const safariRef = useRef(null);

	if (playbackInfo) {
		
		// The play button can/ should be basically full screen - its job is to click farm to avoid autoplay blocking of audio.
		
		// Todo: video scrubber (playback timeline).
		// video length is playbackInfo.duration but note that it might actually be "live" (playbackInfo.isLive is true) 
		// in which case this duration is a snapshot and will continuously grow.

		return <Playback info={playbackInfo} onPlay={() => props.startPlayback()} onPause={() => props.stopPlayback()} />;
	}
	
	var isHost = props.isHost;
	var videoClass = "btn huddle-chat__button huddle-chat__button--camera ";
	videoClass += videoOn ? "btn-success" : "btn-outline-danger";

	var audioClass = "btn huddle-chat__button huddle-chat__button--mute ";
	audioClass += audioOn ? "btn-success" : "btn-outline-danger";

	var shareClass = "btn huddle-chat__button huddle-chat__button--share ";
	shareClass += shareOn ? "btn-primary btn-pulse" : "btn-outline-primary";

	var recordingOn = recordMode == 1;
	var recordingAvailable =  !huddleClient.huddle.huddleType || huddleClient.selfRole() != 3
	if(disableRecording !== undefined && disableRecording === true) {
		recordingAvailable = false;
	}
	var recordClass = "btn huddle-chat__button huddle-chat__button--record ";
	recordClass += recordingOn ? "btn-danger btn-pulse" : "btn-outline-danger";

	var leaveJsx = <>
		<i className="fas fa-phone-slash" />
	</>;

	useEffect(() => {

		if (firefoxRef && firefoxRef.current) {
			setFirefox(!(firefoxRef.current.offsetParent === null));
		}

		if (safariRef && safariRef.current) {
			setSafari(!(safariRef.current.offsetParent === null));
		}

	}, []);

	return <>
		<div className="huddle-chat__footer">
			<div className="huddle-chat__footer-left">
				{/* screen sharing not currently available on mobile */}
				{global.navigator.mediaDevices.getDisplayMedia && <>
					<div className="huddle-chat__button-wrapper">
						<button type="button" className={shareClass} title={shareOn ? `Stop sharing` : `Share your screen`} onClick={() => {
							props.setShare(shareOn ? 0 : 1, SHARE_CANCELLED)
								.then(result => {
									if (result === SHARE_CANCELLED && (firefox || safari)) {
										setScreenshareCancelledShown(true);
                                    }
								});
						}}>
							<i className="fas fa-share-square" />
						</button>
						<span className="huddle-chat__button-label">
							{shareOn ? `Stop sharing` : `Share`}
						</span>
					</div>
				</>}
				{recordingAvailable && <>
					<div className="huddle-chat__button-wrapper">
						<button type="button" className={recordClass} title={shareOn ? `Stop recording` : `Record meeting`}
							onClick={() => {
								recordingOn ? props.onRecordMode(0) : setRecordConfirmShown(true)
							}}>
							<i className="fas fa-circle" />
						</button>
						<span className="huddle-chat__button-label">
							{recordingOn ? `Stop` : `Record`}
						</span>
					</div>
				</>}
			</div>

			<div className="huddle-chat__footer-media">
				<div className="huddle-chat__button-wrapper">
					<button className={videoClass} title={videoOn ? `Turn off camera` : 'Turn on camera'}
						onClick={() => props.setVideo(videoOn ? 0 : 1)}>
						<i className={videoOn ? "fas fa-video" : "fas fa-video-slash"} />
					</button>
					<span className="huddle-chat__button-label">
						{videoOn ? `Camera on` : `Camera off`}
					</span>
				</div>
				<div className="huddle-chat__button-wrapper">
					<button className={audioClass} title={audioOn ? `Mute` : 'Turn on microphone'}
						onClick={() => props.setAudio(audioOn ? 0 : 1)}>
						<i className={audioOn ? "fas fa-microphone" : "fas fa-microphone-slash"} />
					</button>
					<span className="huddle-chat__button-label">
						{audioOn ? `Active` : `Muted`}
					</span>
				</div>
			</div>
			
			{!isHost && <>
				<div className="huddle-chat__button-wrapper">
					<button type="button" className="btn btn-danger huddle-chat__button huddle-chat__button--hangup" title={`Leave meeting`} onClick={() => props.onLeave(1)}> 
						<i className="fas fa-phone-slash" />
					</button>
					<span className="huddle-chat__button-label">
						{`Leave meeting`}
					</span>
				</div>
			</>}
			{isHost && <>
				<Dropdown label={leaveJsx} variant="danger" position="top" align="middle" className="huddle-chat__footer-leave">
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => props.onLeave(1)}>
							<Alert variant="warning">
								<strong>
							{`Leave meeting`}
								</strong>
								<p>
									{`Exit this meeting, leaving it available for other attendees to continue.`}
								</p>
							</Alert>
						</button>
					</li>
					<li>
						<button type="button" className="btn dropdown-item" onClick={() => props.onLeave(3)}>
							<Alert variant="danger">
								<strong>
							{`End meeting`}
								</strong>
								<p>
									{`Close this meeting, disconnecting all other attendees.`}
								</p>
							</Alert>
						</button>
					</li>
				</Dropdown>
			</>}
			
		</div>
		{recordConfirmShown && <>
			<Modal visible className="record-confirm-modal" title={`Record Meeting`} onClose={() => setRecordConfirmShown(false)}>
				<p>{`This will begin recording this meeting.`}</p>
				<p>{`Are you sure you wish to do this?`}</p>
				<footer>
					<button type="button" className="btn btn-danger" onClick={() => {
						props.onRecordMode(1);
						setRecordConfirmShown(false);
					}}>
						{`Record`}
					</button>
					<button type="button" className="btn btn-outline-danger" onClick={() => setRecordConfirmShown(false)}>
						{`Cancel`}
					</button>
				</footer>
			</Modal>
		</>}
		{screenshareCancelledShown && <>
			<Modal visible isLarge className="screenshare-cancelled-modal" title={`Screenshare Cancelled`} onClose={() => setScreenshareCancelledShown(false)}>
				<p>{`The screenshare session was previously cancelled.`}</p>
				<p>{`To restore screenshare permissions:`}</p>
				{firefox && <>
					<ul>
						<li>
							{`Click the permissions icon in the browser address bar (see below)`}
						</li>
						<li>
							{`Click "Blocked" or "Blocked Temporarily" listed next to "Share this screen" to restore screen-sharing permissions`}
						</li>
					</ul>
					{getRef(firefoxPermissions, { attribs: { className: 'firefox-permissions' } })}
				</>}
				{safari && <>
					<ul>
						<li>
							{`Click Safari > Preferences from the main menu:`}
							{getRef(safariPreferences, { attribs: { className: 'safari-preferences' } })}
						</li>
						<li>
							{`Select "Websites", then "Screen Sharing" from the left-hand menu:`}
							{getRef(safariWebsites, { attribs: { className: 'safari-websites' } })}
						</li>
						<li>
							{`Find "` + location.hostname + `" in the list, then click "Deny" to reset the permissions`}
						</li>
					</ul>
				</>}
				<footer>
					<button type="button" className="btn btn-primary" onClick={() => setScreenshareCancelledShown(false)}>
						{`Close`}
					</button>
				</footer>
			</Modal>
		</>}
		<div ref={firefoxRef} className="huddle-chat__footer--firefox"></div>
		<div ref={safariRef} className="huddle-chat__footer--safari"></div>
	</>;
}