import Loading from 'UI/Loading';
import Alert from 'UI/Alert';

var filterDefaultDuplicates = (devices) => {
	var duplicateDevice = null;

	devices.forEach(device => {
		var defaultLabel = device.label ? ('Default - ' + device.label) : null;
		var hasDefault = defaultLabel ? devices.filter(d => d.label && d.label == defaultLabel) : false;

		if (hasDefault.length) {
			duplicateDevice = device;
        }
	});

	if (duplicateDevice) {
		const index = devices.indexOf(duplicateDevice);

		if (index > -1) {
			devices.splice(index, 1);
		}
    }

	return devices;
};

var collectDevices = () => {
	
	return new Promise((s, r) => {
		
		navigator.mediaDevices.enumerateDevices().then(devices => {
		
			var deviceSets = { audio: [], video: [] };

			deviceSets.audio = filterDefaultDuplicates(devices.filter(device => device.kind == 'audioinput'));
			deviceSets.video = filterDefaultDuplicates(devices.filter(device => device.kind == 'videoinput' || device.kind == 'videooutput'));

			s(deviceSets);
		}).catch(e => {
			s({failed: true, error: e});
		});
	});
};

/*
Mic/ camera test UI. Appears on startup and is available via the cog in the corner at any point onwards.
*/
export default class AvTest extends React.Component{
	
	constructor(props){
		super(props);
		this.state = {};
		
		this.onDevicesChanged = this.onDevicesChanged.bind(this);
		this.feedAmplitude = this.feedAmplitude.bind(this);
		this.renderAudioOptions = this.renderAudioOptions.bind(this);
		this.renderCameraOptions = this.renderCameraOptions.bind(this);
	}
	
	componentDidMount(){
		navigator.mediaDevices.addEventListener('devicechange', this.onDevicesChanged); 
		var html = document.querySelector("html");

		// enable devices
		this.setState({
			enableMic: true,
			enableCamera: true
		});

		// remove padding for fixed header
		html.classList.add("disable-header-padding");

		// Initial run:
		this.onDevicesChanged();
	}
	
	componentWillUnmount(){
		navigator.mediaDevices.removeEventListener('devicechange', this.onDevicesChanged);
		var html = document.querySelector("html");

		// restore padding for fixed header
		html.classList.remove("disable-header-padding");
	}
	
	feedAmplitude(avg, proc) {
		
		var canvas = this.micMeterRef;
		
		if(canvas){
			var canvasCtx = canvas.getContext('2d');
			canvasCtx.fillStyle = 'rgb(255, 255, 255)';
			canvasCtx.fillRect(0, 0, canvas.width, canvas.height);

			if (this.state.enableMic) {
				canvasCtx.fillStyle = '#28a745';
				canvasCtx.fillRect(0, 0, canvas.width * avg, canvas.height);
            }
		}
		
	}
	
	setSelectedCamWithOpen(deviceId) {
		var constraints = {
			video : {
				width          : { max: 1920/2, ideal: 1920/2 },
				height         : { max: 1080/2, ideal: 1080/2 },
				frameRate      : { max: 30 },
				deviceId
			},
			audio: false,
		};
		
		global.navigator.mediaDevices.getUserMedia(constraints).then(stream => {
			
			if(this.videoSpaceRef){
				this.videoSpaceRef.srcObject = stream;
				
				this.videoSpaceRef.play().catch(e => {
					console.error(e);
				});
			}
			
			stream._stop = () => {
				stream.getTracks().forEach(t => t.stop());
			};
			
			this.setState({selectedCam:{deviceId, stream}});
			
		}).catch(e => {
			console.error(e);
		});
	}
	
	setSelectedMicWithOpen(deviceId, icon) {
		
		// Open it:
		global.navigator.mediaDevices.getUserMedia({audio: {
				deviceId,
				autoGainControl: false,
				noiseSuppression: false,
				echoCancellation: false,
				typingNoiseDetection: false,
				audioMirroring: false,
				highpassFilter: false
			}}).then(stream => {
			var AudioContext = window.AudioContext || window.webkitAudioContext;
			var audioCtx = new AudioContext();
			var analyser = audioCtx.createAnalyser();
			var scriptProcessor = audioCtx.createScriptProcessor(2048, 1, 1);
			var amplitudeArray = new Float32Array(analyser.fftSize);
			var sourceNode = audioCtx.createMediaStreamSource(stream);
			var mixedOutput = audioCtx.createMediaStreamDestination();
			
			sourceNode.connect(analyser);
			analyser.connect(scriptProcessor);
			scriptProcessor.connect(mixedOutput);
			
			scriptProcessor.onaudioprocess = () => {
				analyser.getFloatTimeDomainData(amplitudeArray);

				var maxVal = 0;

				for(var i=0;i<amplitudeArray.length;i++){
				var currentVal = amplitudeArray[i];

				if(currentVal > maxVal){
					maxVal = currentVal;
				}
				}

				if(maxVal < 0){
					maxVal = -maxVal;
				}

				this.feedAmplitude(maxVal, scriptProcessor);
			};
			
			var s = mixedOutput.stream;
			s._stop = () => {
				stream.getTracks().forEach(t => t.stop());
				audioCtx.close();
			};
			
			if(this.state.selectedMic && this.state.selectedMic.stream){
				// Stop existing ones
				this.state.selectedMic.stream._stop();
			}

			this.setState({selectedMic: {deviceId, stream: s, icon}});
		}).catch(e => {
			console.error(e);
		});
	}
	
	onDevicesChanged () {
		collectDevices().then(devices => {
			
			var selectedDevices = {};
			
			if (!this.state.selectedMic && devices.audio && devices.audio.length) {
				var initialDevice = devices.audio[0];
				var dev = initialDevice.deviceId;
				var icon = (initialDevice.label && initialDevice.label.toLowerCase().includes("headset")) ? "fa-headset" : "fa-microphone";

				this.setSelectedMicWithOpen(dev, icon);
				selectedDevices.deviceIdAudio = dev;
			}
			
			if(!this.state.selectedCam && devices.video && devices.video.length){
				var dev = devices.video[0].deviceId;
				this.setSelectedCamWithOpen(dev);
				selectedDevices.deviceIdVideo = dev;
			}
			
			if(selectedDevices.deviceIdAudio || selectedDevices.deviceIdVideo){
				this.updateSelections(selectedDevices);
			}
			
			this.setState({devices});
		});
	}
	
	updateSelections(devices) {
		this.props.onDeviceSelect && this.props.onDeviceSelect(devices);
	}

	renderAudioOptions(selectedMic, audioDevices) {

		if (!audioDevices || audioDevices.length == 0) {
			return <div className="mb-3">
				<label className="form-label form-label--disabled">
					<i className="fas fa-fw fa-2x fa-microphone"></i>
					{`No micophone detected`}
				</label>
			</div>;
		}

		var icon = "fas fa-fw fa-2x " + (this.state.selectedMic ? this.state.selectedMic.icon : "fa-microphone");

		return <>
			<div className="mb-3">
				<label htmlFor="av-test__microphone-select" className="form-label">
					<i className={icon}></i>
					{`Which microphone would you like to use?`}
				</label>

				{audioDevices.map(device => {
					return <div className="form-check">
						<input className="form-check-input" type="radio" name="microphone" id={device.deviceId}
							checked={selectedMic && (selectedMic.deviceId == device.deviceId) ? true : undefined}
							onChange={e => {
								var selectedDeviceId = e.target.id;
								var icon = (device.label && device.label.toLowerCase().includes("headset")) ? "fa-headset" : "fa-microphone";

								this.setSelectedMicWithOpen(selectedDeviceId, icon);
								this.updateSelections({ deviceIdAudio: selectedDeviceId });
							}} />
						<label className="form-check-label" htmlFor={device.deviceId}>
							{device.label || `Unnamed audio device`}
						</label>
					</div>
				})}
			</div>
			<div className="av-test__mic-test">
				<div className="form-check form-switch">
					<input className="form-check-input" type="checkbox" role="switch" id="enableMicrophone" checked={this.state.enableMic ? true : undefined}
						onChange={() => {
							var enableMic = !this.state.enableMic;
							this.setState({ enableMic  });
							this.updateSelections({ audioInitiallyDisabled: !enableMic });
						}} />
					<label className="form-check-label" htmlFor="enableMicrophone">
						{`Enable microphone`}
					</label>
				</div>
				<canvas className={this.state.enableMic ? "av-test__microphone-volume" : "av-test__microphone-volume av-test__microphone-volume--disabled"}
					ref={r => this.micMeterRef = r} width={180} height={20} />
			</div>
		</>;
    }

	renderCameraOptions(selectedCam, videoDevices) {

		if (!videoDevices || videoDevices.length == 0) {
			return <div className="mb-3">
				<label className="form-label form-label--disabled">
					<i className="fas fa-fw fa-2x fa-camera-home"></i>
					{`No camera detected`}
				</label>
			</div>;
		}

		return <>
			<div className="mb-3">
				<label htmlFor="av-test__camera-select" className="form-label">
					<i className="fas fa-fw fa-2x fa-camera-home"></i>
					{`Which camera would you like to use?`}
				</label>

				{videoDevices.map(device => {
					return <div className="form-check">
						<input className="form-check-input" type="radio" name="camera" id={device.deviceId}
							checked={selectedCam && (selectedCam.deviceId == device.deviceId) ? true : undefined}
							onChange={e => {
								var selectedDeviceId = e.target.id;
								this.setSelectedCamWithOpen(selectedDeviceId);
								this.updateSelections({ deviceIdVideo: selectedDeviceId });
							}} />
						<label className="form-check-label" htmlFor={device.deviceId}>
							{device.label || `Unnamed video device`}
						</label>
					</div>
				})}
			</div>
			<div className="av-test__cam-test">
				<div className="form-check form-switch">
					<input className="form-check-input" type="checkbox" role="switch" id="enableCamera" checked={this.state.enableCamera ? true : undefined}
						onChange={() => {
							var enableCamera = !this.state.enableCamera;
							this.setState({ enableCamera });
							this.updateSelections({ videoInitiallyDisabled: !enableCamera });

							if (!this.state.enableCamera && this.state.selectedCam) {
								this.setSelectedCamWithOpen(this.state.selectedCam.deviceId);
                            }

						}} />
					<label className="form-check-label" htmlFor="enableCamera">
						{`Enable camera`}
					</label>
				</div>
				{this.state.enableCamera && <>
					<video className="av-test__video-sample" ref={v => this.videoSpaceRef = v} />
				</>}
			</div>
		</>;
    }

	render() {
		
		var {devices} = this.state;
		
		if(!devices){
			// Collecting device set.
			return <Loading message={`Searching for a camera and microphone`} />;
		}

		if (devices.failed) {
			return <div className="huddle-lobby--devices-failed">
				<Alert variant="error">
					<strong>{`Unable to access your camera or microphone devices.`}</strong><br />
					{`This is usually because either none were plugged in or because the permission prompt was rejected.`}<br/>
					{`To participate in the meeting you'll need at least a microphone.`}
				</Alert>
				<button className="btn btn-primary" onClick={() => {
					// Run the func again:
					this.setState({ devices: null });
					this.onDevicesChanged();
				}}>
					{`Search again`}
				</button>
			</div>;
		}

		if (this.props.huddleReadyCallback) {
			this.props.huddleReadyCallback(true);
		}

		var {selectedMic, selectedCam} = this.state;

		console.log("MIC: ", selectedMic);

		// Got a device list. Display the UI now:
		return <div className="av-test">
			<h1 className="av-test__title">
				{`Huddle meeting`}
			</h1>
			<div className="av-test__devices">
				{/* microphone */}
				<div className="av-test__devices--microphone">
					{this.renderAudioOptions(selectedMic, devices.audio)}
				</div>

				<hr />

				{/* camera */}
				<div className="av-test__devices--camera">
					{this.renderCameraOptions(selectedCam, devices.video)}
				</div>
			</div>
		</div>;
	}
}