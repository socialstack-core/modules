import Alert from 'UI/Alert';

export default class VideoCapture extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            recording: false,
            video: null,
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
		
		if(this.video){
			this.video.srcObject = null;
		}
	}
	
    startRecording(e) {
        e.preventDefault();
		
		if(!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia){
			this.setState({noStream:true});
			return;
		}
		
		navigator.mediaDevices.getUserMedia({video: {facingMode: "environment"}, audio: true, facingMode: "environment"}).then(stream => {
            // show it to user
			
			if(typeof global.MediaRecorder === undefined){
				this.setState({noStream:true});
				return;
			}
			
			this.video.srcObject = stream;
            this.video.play();
			
			// Fullscreen:
			if(this.videoHost && this.videoHost.requestFullscreen){
				this.videoHost.requestFullscreen();
			}
			
            // init recording
            var mediaRecorder = new global.MediaRecorder(stream);
			
			// wipe old data chunks
			this.chunks = [];
			
            // listen for data from media recorder
            mediaRecorder.ondataavailable = e => {
                if (e.data && e.data.size > 0) {
                    this.chunks.push(e.data);
                }
            };
			
			this.props.onRecordingState && this.props.onRecordingState(true);
			
			// start recorder with 10ms buffer
			mediaRecorder.start(10);
            this.setState({mediaRecorder, recording: true, stream});
			
        }).catch(e => {
			console.log(e);
			this.setState({noStream:true});
			
		});
    }
    
    stopRecording(e) {
        e.preventDefault();
		
		const { mediaRecorder } = this.state;
		
		if(!mediaRecorder){
			return;
		}
		
		if(this.videoHost && this.videoHost.requestFullscreen){
			global.document.exitFullscreen();
		}
		
		var type = mediaRecorder.mimeType;
		var mimeType = type.split(';')[0];
		
        // stop the recorder
        mediaRecorder.stop();
		
		if(this.state.stream){
			this.state.stream.getTracks().forEach( track => track.stop() );
		}
		
		this.props.onRecordingState && this.props.onRecordingState(false);
		
        // say that we're not recording
        this.setState({recording: false, mediaRecorder: null, stream: null});
		
        // save to memory
        this.save(mimeType, type);
    }
    
    save(mimeType, type) {
        // convert saved chunks to blob
        const blob = new Blob(this.chunks, {type: 'video/*'});
		this.video.srcObject = null;
        // generate url from blob
        const url = window.URL.createObjectURL(blob);
        // append URL to list of saved content for rendering
		this.props.onChange && this.props.onChange(blob, mimeType);
        this.setState({video: url});
    }
    
    deleteMedia() {
        this.setState({video:null});
		this.props.onChange && this.props.onChange(null);
    }
    
    render() {
        const {recording, video, noStream} = this.state;
		
		if(noStream){
			return <div className="video">
				<Alert type="error">
					Either no supported recording device, or permission was denied.
				</Alert>
			</div>;
		}
		
        return (
            <div ref={vidRef => this.videoHost = vidRef} className="video"> 
                <div>
                    {video != null ? <div style = {{width: "100%"}} className = "btn btn-primary" onClick = {e => {this.deleteMedia()}}><i className="fas fa-trash-alt" /> Delete Video</div> : 
                    !recording ? <div style = {{width: "100%"}} className = "btn btn-primary" onClick={e => this.startRecording(e)}><i className="fas fa-video" /> Record Video</div> :
                    recording && <div style = {{width: "100%", color: "black", backgroundColor: "#FFEB3B"}} className = "btn btn-primary" onClick={e => this.stopRecording(e)}><i className="fas fa-stop" /> Stop</div>}
                </div>
                <div>
					{video ? (
						<video controls style={{width: "100%"}} src={video}  />
					) : (
						<video ref={ref => this.video = ref} controls style={{width: "100%", display: recording ? 'block' : 'none'}}   />
					)}
                    
                </div>
            </div>
        );
    }
}