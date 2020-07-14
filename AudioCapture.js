import Alert from 'UI/Alert';
import AudioPolyfill from 'UI/Functions/AudioPolyfill';

export default class AudioCapture extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            recording: false,
            audio: null,
            mediaRecorder: null
        };
    }
    
	componentWillUnmount(){
		
		if(this.state.mediaRecorder){
			this.state.mediaRecorder.stop();
		}
		
		if(this.state.stream){
			this.state.stream.getTracks().forEach( track => track.stop() );
		}
		
	}
	
	startIOS(){
		
		global.audioinput.checkMicrophonePermission((hasPermission) => {
			if (hasPermission) {
				this.initRecording(null);
			} else {	        
				// Ask the user for permission to access the microphone
				global.audioinput.getMicrophonePermission((hasPermission, message) => {
					if (hasPermission) {
						this.initRecording(null);
					} else {
						this.setState({noStream:true, recording: false});
						this.props.onRecordingState && this.props.onRecordingState(false);
					}
				});
			}
		});
		
	}
	
    startRecording(e) {
        e.preventDefault();
		
		if(global.cordova && global.audioinput && global.cordova.platformId == 'ios'){
			// iOS native workaround
			this.startIOS();
			return;
		}
		
		if(!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia){
			this.setState({noStream:true, recording: false});
			this.props.onRecordingState && this.props.onRecordingState(false);
			return;
		}
		
		navigator.mediaDevices.getUserMedia({audio: true, video: false}).then(stream => {
            
			this.initRecording(stream);
			
        }).catch(e => {
			console.log(e);
			this.setState({noStream:true, recording: false});
			this.props.onRecordingState && this.props.onRecordingState(false);
		});
    }
    
	initRecording(stream){
		
		// init recording
		var mediaRecorder = new AudioPolyfill(stream);
		
		mediaRecorder.onstopped = () => {
			// It hit the time limit
			this.stopRecording();
		};
		
		this.props.onRecordingState && this.props.onRecordingState(true);
		
		// start recorder with 10ms buffer
		mediaRecorder.start(10);
		this.setState({mediaRecorder, recording: true, stream});
		
	}
	
    stopRecording(e) {
        e && e.preventDefault();
		
		const { mediaRecorder } = this.state;
		
		if(!mediaRecorder){
			return;
		}
		
		var type = mediaRecorder.mimeType;
		var mimeType = type.split(';')[0];
		
        // stop the recorder
        mediaRecorder.stop();
		
		if(this.state.stream){
			this.state.stream.getTracks().forEach( track => track.stop() );
		}
		
        // say that we're not recording
        this.setState({recording: false, mediaRecorder: null, stream: null});
		
		this.props.onRecordingState && this.props.onRecordingState(false);
		
        // save to memory
        this.save(mimeType, type);
    }
    
    save(mimeType, type) {
        // convert saved chunks to blob
		let blob = null;
		
		if(this.state.mediaRecorder.getWav){
			blob = new Blob([this.state.mediaRecorder.getWav()], {type: 'audio/wav'});
		}
		
        // generate url from blob
        const url = window.URL.createObjectURL(blob);
        // append URL to list of saved content for rendering
		this.props.onChange && this.props.onChange(blob, mimeType);
        this.setState({audio: url});
    }
    
    deleteMedia() {
        this.setState({audio:null});
		this.props.onChange && this.props.onChange(null);
    }
    
    render() {
        const {recording, audio, noStream} = this.state;
		
		if(noStream){
			return <div className="audio">
				<Alert type="error">
					Either no supported recording device, or permission was denied.
				</Alert>
			</div>;
		}
		
        return (
            <div className="audio">
                <div>
                    {audio != null ? <div style = {{width: "100%"}} className = "btn btn-primary" onClick = {e => {this.deleteMedia()}}><i className="fas fa-trash-alt" /> Delete Audio</div> : 
                    !recording ? <div style = {{width: "100%"}} className = "btn btn-primary" onClick={e => this.startRecording(e)}><i className="fas fa-microphone" /> Record Audio</div> :
                    recording && <div style = {{width: "100%", color: "black", backgroundColor: "#FFEB3B"}} className = "btn btn-primary" onClick={e => this.stopRecording(e)}><i className="fas fa-stop" /> Stop</div>}
                </div>
                <div>
                    {audio != null && <div key={`audio`}>
                        <audio controls style={{width: "100%"}} src={audio}   />
                    </div>
                    }
                </div>
            </div>
        );
    }
}