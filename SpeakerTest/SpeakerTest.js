import sampleFileRef from './static/speaker-test.mp3';
import getRef from 'UI/Functions/GetRef';

export default class SpeakerTest extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	stop(){
		var { audio } = this.state;
		if(!audio){
			return;
		}
		
		audio.pause();
		this.setState({audio: null});
	}
	
	componentWillUnmount(){
		// Ensure any playing audio is stopped.
		this.stop();
	}
	
	play(){
		var audio = new Audio();
		this.setState({audio});
		audio.src = getRef(sampleFileRef, {url: 1});
		audio.play();
	}
	
	render(){
		var {audio} = this.state;
		
		return <div>
			<p>
				Test your speakers
			</p>
			{audio ? (
				<button className="btn btn-secondary" onClick={() => this.stop()}>Stop</button>
			) : (
				<button className="btn btn-secondary" onClick={() => this.play()}>Play</button>
			)}
		</div>;
	}
	
} 