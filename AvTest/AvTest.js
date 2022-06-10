
var collectDevices = () => {
	
	return new Promise((s, r) => {
		
		navigator.mediaDevices.enumerateDevices().then(devices => {
		
			var deviceSets = {audio: [], video: []};
			
			devices.forEach(device => {
			
				if (device.kind == 'audioinput'){
					deviceSets.audio.push(device);
				} else if (device.kind == 'videoinput' || device.kind == 'videooutput'){
					deviceSets.video.push(device);
				}
				
			});
			
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
	}
	
	componentDidMount(){
		navigator.mediaDevices.addEventListener('devicechange', this.onDevicesChanged); 
		
		// Initial run:
		this.onDevicesChanged();
	}
	
	componentWillUnmount(){
		navigator.mediaDevices.removeEventListener('devicechange', this.onDevicesChanged);
	}
	
	feedAmplitude(avg, proc) {
		
		var canvas = this.micMeterRef;
		
		if(canvas){
			var canvasCtx = canvas.getContext('2d');
			canvasCtx.fillStyle = 'rgb(255, 255, 255)';
			canvasCtx.fillRect(0, 0, canvas.width, canvas.height);
			canvasCtx.fillStyle = '#28a745';
			canvasCtx.fillRect(0, 0, canvas.width * avg, canvas.height);
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
	
	setSelectedMicWithOpen (deviceId) {
		
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
			
			this.setState({selectedMic: {deviceId, stream: s}});
			
		}).catch(e => {
			console.error(e);
		});
	}
	
	onDevicesChanged () {
		collectDevices().then(devices => {
			
			var selectedDevices = {};
			
			if(!this.state.selectedMic && devices.audio && devices.audio.length){
				var dev = devices.audio[0].deviceId;
				this.setSelectedMicWithOpen(dev);
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
	
	updateSelections(devices){
		this.props.onDeviceSelect && this.props.onDeviceSelect(devices);
	}
	
	render(){
		
		var {devices} = this.state;
		
		if(!devices){
			// Collecting device set.
			return `Searching for a camera and microphone..`;
		}

		if(devices.failed){
			
			return <center>
				Unable to access your camera or microphone devices. 
				This is usually because either none were plugged in or because the permission prompt was rejected. 
				To participate in the meeting you'll need at least a microphone.
				<p>
					<button className="btn btn-primary" onClick={() => {
						// Run the func again:
						this.setState({devices: null});
						this.onDevicesChanged();
					}}>
						Search again
					</button>
				</p>
			</center>;
			
		}
		
		var {selectedMic, selectedCam} = this.state;
		
		// Got a device list. Display the UI now:
		return <div className="av-test">
			<div>
				<label for='av-test_microphone_select'>
					Which microphone would you like to use?
				</label>
				<select name='microphone' id='av-test_microphone_select' value={selectedMic ? selectedMic.deviceId : undefined} onChange={e => {
					
					this.setSelectedMicWithOpen(e.target.value);
					
					this.updateSelections({deviceIdAudio: e.target.value});
				}}>
					{devices.audio.map(device => {
						return <option value={device.deviceId}>{device.label || 'Unnamed audio device'}</option>
					})}
				</select>
				<canvas className="av-test__microphone-volume" ref={r => this.micMeterRef =r} width={180} height={20} />
			</div>
			<div>
				<label for='av-test_camera_select'>
					Which camera would you like to use?
				</label>
				<select name='camera' id='av-test_camera_select' value={selectedCam ? selectedCam.deviceId : undefined} onChange={e => {
					
					this.setSelectedCamWithOpen(e.target.value);
					this.updateSelections({deviceIdVideo: e.target.value});
					
				}}>
					{devices.video.map(device => {
						return <option value={device.deviceId}>{device.label || 'Unnamed video device'}</option>
					})}
				</select>
				<video className="av-test__video-sample" ref={v => this.videoSpaceRef=v} />
			</div>
		</div>;
	}
}