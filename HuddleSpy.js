// Module import examples - none are required:
import HuddleClient from 'UI/Functions/HuddleClient';
// import Loop from 'UI/Loop';
import Peer from 'UI/HuddleSpy/Peer';

// Previews the current audio of public huddles
export default class HuddleSpy extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {};
		this.state.huddleClient = this.connectTo(this.props.roomId || 1, true);
		this.onRoomUpdate = this.onRoomUpdate.bind(this);
	}
	
	connectTo(room, ret){
		var {huddleClient} = this.state;
		
		if(this._room == room){
			return huddleClient;
		}
		
		this._room = room;
		
		if(huddleClient){
			huddleClient.close();
		}
		
		var client = new HuddleClient({
			roomId: room.toString(),
			produce: false,
			consume: true,
			useSimulcast: true,
			useSharingSimulcast: true
		});
		
		if(ret){
			return client;
		}
		
		this.setState({
			huddleClient: client
		});
	}
	
	componentWillReceiveProps(props){
		this.connectTo(props.roomId);
	}
	
	onRoomUpdate(evt)
	{
		this.setState({huddleClient: this.state.huddleClient});
	}
	
	componentDidMount()
	{
		const { huddleClient } = this.state;
		huddleClient.addEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.join();
		
	}
	
	componentWillUnmount()
	{
		const { huddleClient } = this.state;
		huddleClient.removeEventListener('roomupdate', this.onRoomUpdate);
		huddleClient.close();
	}
	
	render(){
		const {
			huddleClient
		} = this.state;
		
		var peers = huddleClient.peers;
		
		if(!peers){
			return;
		}
		
		return <div className="huddle-spy">
			{/*
				Optionally display connected state indicator
			*/}
			{
				peers.map((peer) =>
				{
					const audioConsumer = peer.consumers.find((consumer) => consumer.track.kind === 'audio');
					
					return <Peer peer={peer} audio={audioConsumer ? audioConsumer.track : null} />;
				})
			}
		</div>;
		
	}
	
}

HuddleSpy.propTypes = {
	roomId: 'int'
};
