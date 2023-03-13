var Constant = null;

export default class Executor {
	constructor(props){
		this.state = {};
		this.props = props;
		
		this.outputs = {};
	}
	
	loadData(d){
		// Loads data from the given JSON deserialised object into this executor node.
		// Usually just loads them as a collection of Constant objects. Constant itself retains the original value.
		if(d){
			if(!Constant){
				Constant = require('UI/Canvas/Graph/Constant').default;
			}
			
			for(var key in d){
				this.state[key] = new Constant({output: d[key]});
			}
		}
	}
	
	reset(){
		// Clears any cached state if necessary.
	}
	
	async run(){
		var result = await this.go();
		
		// Set output and return it:
		this.outputs.output = result;
		return result;
	}
}