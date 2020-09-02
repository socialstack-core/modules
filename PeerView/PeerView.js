import Loading from 'UI/Loading';

export default class PeerView extends React.Component {
	constructor(props)
	{
		super(props);

		this.state =
		{
			videoResolutionWidth  : null,
			videoResolutionHeight : null,
			videoCanPlay          : false,
			videoElemPaused       : false,
			maxSpatialLayer       : null
		};

		// Latest received video track.
		// @type {MediaStreamTrack}
		this._audioTrack = null;

		// Latest received video track.
		// @type {MediaStreamTrack}
		this._videoTrack = null;
		
		// Periodic timer for reading video resolution.
		this._videoResolutionPeriodicTimer = null;
		
		this.refs = {};
	}

	render()
	{
		const {
			isMe,
			displayName,
			peer,
			audioMuted,
			videoVisible,
			videoMultiLayer,
			consumerCurrentSpatialLayer,
			videoScore
		} = this.props;

		const {
			videoResolutionWidth,
			videoResolutionHeight,
			videoCanPlay,
			videoElemPaused,
			maxSpatialLayer
		} = this.state;
		
		var videoClassNames = '';
		
		if(isMe){
			videoClassNames += 'is-me ';
		}
		
		if(!videoVisible || !videoCanPlay){
			videoClassNames += 'peerview-hidden ';
		}
		
		if(videoVisible && videoMultiLayer && consumerCurrentSpatialLayer === null){
			videoClassNames += 'network-error';
		}

		//var avatarUrl = user && user.avatarRef ? getRef(user.avatarRef, { url: true, size: 100 }) : "";
		//console.log(isMe ? "ME: " : "PEER: ", peer);
		//console.log("name: ", displayName);
		
		return (
			<div className='peerView'>

			{/*
				<div className='info'>
					<div className={'peer ' + (isMe ? 'is-me' : '')}>
						<span className='display-name'>
							{isMe ? displayName : peer.displayName}
						</span>
					</div>
				</div>
			*/}
				<video
					ref={r => this.refs.videoElem = r}
					className={videoClassNames}
					autoPlay
					playsInline
					muted
					controls={false}
				/>

				<audio
					ref={r => this.refs.audioElem = r}
					autoPlay
					playsInline
					muted={isMe || audioMuted}
					controls={false}
				/>
				
				{(videoVisible && videoScore < 5) && (
					<div className='spinner-container'>
						<Loading />
					</div>
				)}

				{videoElemPaused && (
					<div className='video-elem-paused' />
				)}
			</div>
		);
	}

	componentDidMount()
	{
		const { audioTrack, videoTrack } = this.props;

		this._setTracks(audioTrack, videoTrack);
	}

	componentWillUnmount()
	{
		clearInterval(this._videoResolutionPeriodicTimer);

		const { videoElem } = this.refs;

		if (videoElem)
		{
			videoElem.oncanplay = null;
			videoElem.onplay = null;
			videoElem.onpause = null;
		}
	}

	componentWillUpdate()
	{
		const {
			isMe,
			audioTrack,
			videoTrack,
			videoRtpParameters
		} = this.props;

		const { maxSpatialLayer } = this.state;

		if (isMe && videoRtpParameters && maxSpatialLayer === null)
		{
			this.setState(
				{
					maxSpatialLayer : videoRtpParameters.encodings.length - 1
				});
		}
		else if (isMe && !videoRtpParameters && maxSpatialLayer !== null)
		{
			this.setState({ maxSpatialLayer: null });
		}

		this._setTracks(audioTrack, videoTrack);
	}

	_setTracks(audioTrack, videoTrack)
	{
		if (this._audioTrack === audioTrack && this._videoTrack === videoTrack)
			return;

		this._audioTrack = audioTrack;
		this._videoTrack = videoTrack;

		this._stopVideoResolution();

		const { audioElem, videoElem } = this.refs;

		if (audioTrack)
		{
			const stream = new MediaStream;

			stream.addTrack(audioTrack);
			audioElem.srcObject = stream;

			audioElem.play()
				.catch((error) => console.warn('audioElem.play() failed:%o', error));
		}
		else
		{
			audioElem.srcObject = null;
		}

		if (videoTrack)
		{
			const stream = new MediaStream;

			stream.addTrack(videoTrack);
			videoElem.srcObject = stream;

			videoElem.oncanplay = () => this.setState({ videoCanPlay: true });

			videoElem.onplay = () =>
			{
				this.setState({ videoElemPaused: false });

				audioElem.play()
					.catch((error) => console.warn('audioElem.play() failed:%o', error));
			};

			videoElem.onpause = () => this.setState({ videoElemPaused: true });

			videoElem.play()
				.catch((error) => console.warn('videoElem.play() failed:%o', error));

			this._startVideoResolution();
		}
		else
		{
			videoElem.srcObject = null;
		}
	}
	
	_startVideoResolution()
	{
		this._videoResolutionPeriodicTimer = setInterval(() =>
		{
			const { videoResolutionWidth, videoResolutionHeight } = this.state;
			const { videoElem } = this.refs;

			if (
				videoElem.videoWidth !== videoResolutionWidth ||
				videoElem.videoHeight !== videoResolutionHeight
			)
			{
				this.setState(
					{
						videoResolutionWidth  : videoElem.videoWidth,
						videoResolutionHeight : videoElem.videoHeight
					});
			}
		}, 500);
	}

	_stopVideoResolution()
	{
		clearInterval(this._videoResolutionPeriodicTimer);

		this.setState(
			{
				videoResolutionWidth  : null,
				videoResolutionHeight : null
			});
	}
	
}

/*
// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

PeerView.propTypes = {
	
	title: 'string', // text input
	size: [1,2,3,4], // dropdowns
	
	// All <Input type='x' /> values are supported - checkbox, color etc.
	// Also the special id type which can be used to select some other piece of content (by peerView name), like this:
	templateToUse: {type: 'id', content: 'Template'}
	
};

PeerView.icon='align-center'; // fontawesome icon
*/