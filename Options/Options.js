import Dropdown from 'UI/Dropdown';
import Alert from 'UI/Alert';
import Playback from 'UI/HuddleChat/Playback';

export default function Options(props) {
	var { audioOn, videoOn, shareOn, isHost, playbackInfo } = props;
	
	if (playbackInfo) {
		
		// The play button can/ should be basically full screen - its job is to click farm to avoid autoplay blocking of audio.
		
		// Todo: video scrubber (playback timeline).
		// video length is playbackInfo.duration but note that it might actually be "live" (playbackInfo.isLive is true) 
		// in which case this duration is a snapshot and will continuously grow.

		return <Playback duration={playbackInfo.duration} isLive={playbackInfo.isLive}
			onPlay={() => props.startPlayback()} onPause={() => props.stopPlayback()} />;
	}
	
	var isHost = props.isHost;
	var videoClass = "btn huddle-chat__button huddle-chat__button--camera ";
	videoClass += videoOn ? "btn-success" : "btn-outline-danger";

	var audioClass = "btn huddle-chat__button huddle-chat__button--mute ";
	audioClass += audioOn ? "btn-success" : "btn-outline-danger";

	var shareClass = "btn huddle-chat__button huddle-chat__button--share ";
	shareClass += shareOn ? "btn-primary btn-pulse" : "btn-outline-primary";

	var leaveJsx = <>
		<i className="fas fa-phone-slash" />
	</>;

	return <div className="huddle-chat__options">
		<div className="huddle-chat__options-left">
			<div className="huddle-chat__button-wrapper">
				<button type="button" className={shareClass} title={shareOn ? `Stop sharing` : `Share your screen`} onClick={() => props.setShare(shareOn ? 0 : 1)}>
					<i className="fas fa-share-square" />
				</button>
				<span className="huddle-chat__button-label">
					{shareOn ? `Stop sharing` : `Share`}
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
			<Dropdown label={leaveJsx} variant="danger" position="top" align="middle" className="huddle-chat__options-leave">
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
		<div className="huddle-chat__options-media">
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
	</div>;
}