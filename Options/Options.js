export default function Options(props){
	
	var {audioOn, videoOn, shareOn} = props;

	var videoClass = "btn huddle-chat__button huddle-chat__button--camera ";
	videoClass += videoOn ? "btn-danger" : "btn-success";

	var audioClass = "btn huddle-chat__button huddle-chat__button--mute ";
	audioClass += audioOn ? "btn-danger btn-pulse" : "btn-success";

	var shareClass = "btn btn-primary huddle-chat__button huddle-chat__button--share ";
	shareClass += shareOn ? "btn-danger btn-pulse" : "btn-primary";

	return <div className="huddle-chat__options">
		<div className="huddle-chat__options-left">
			<button type="button" className={shareClass} title={shareOn ? `Stop sharing` : `Share your screen`} onClick={() => props.setShare(shareOn ? 0 : 1)}>
				<i className="fas fa-share-square" />
			</button>
		</div>
		<button type="button" className="btn btn-danger huddle-chat__button huddle-chat__button--hangup" title={`Leave meeting`}>
			<i className="fas fa-phone-slash" />
		</button>
		<div className="huddle-chat__options-media">
			<button className={videoClass} title={videoOn ? `Turn off camera` : 'Turn on camera'}
				onClick={() => props.setVideo(videoOn ? 0 : 1)}>
				<i className={videoOn ? "fas fa-video-slash" : "fas fa-video"} />
			</button>
			<button className={audioClass} title={audioOn ? `Mute` : 'Turn on microphone'}
				onClick={() => props.setAudio(audioOn ? 0 : 1)}>
				<i className={audioOn ? "fas fa-microphone-slash" : "fas fa-microphone"} />
			</button>
		</div>
	</div>;
}