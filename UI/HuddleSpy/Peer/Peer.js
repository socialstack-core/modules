export default class Peer extends React.Component {
	
	constructor(props)
	{
		super(props);
		this.state ={};
		this._audioTrack = null;
	}
	
	render()
	{
		return <audio
			ref={r => this.audioElem = r}
			autoPlay
			playsInline
			controls={false}
		/>;
	}

	componentDidMount()
	{
		const { audio } = this.props;
		this._setTracks(audio);
	}
	
	componentWillUpdate()
	{
		const { audio } = this.props;
		this._setTracks(audio);
	}
	
	_setTracks(audio)
	{
		if (this._audioTrack === audio)
			return;

		this._audioTrack = audio;

		const { audioElem } = this;

		if (audio)
		{
			const stream = new MediaStream;
			stream.addTrack(audio);
			audioElem.srcObject = stream;
			audioElem.play().catch((error) => console.warn('audioElem.play() failed:%o', error));
		}
		else
		{
			audioElem.srcObject = null;
		}
	}
	
}