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
		audio.loop = true;
		audio.play();
	}
	
	render(){
		var {audio} = this.state;
		
		return <>
			<button className="btn btn-outline-primary" onClick={() => audio ? this.stop() : this.play()}>
				{audio ? <>
					<i className="fas fa-fw fa-volume-slash"></i> {`Stop speaker test`}
				</> : <>
					<i className="fas fa-fw fa-volume"></i> {`Test speakers`}
				</>}
			</button>
		</>;
	}
	
} 