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
	
	onValuesReady(props, ready){
		var proms = props.__proms;
		if(proms){
			delete props.__proms;
		}
		return proms ? Promise.all(proms).then(ready) : ready();
	}
	
	readValue(stateField, into){
		// Runs a state field. If it returns a promise  
		// it will await the promise before putting the result into the given set.
		var sf = this.state[stateField];
		if(!sf){
			return;
		}
		var result = sf.run();
		
		if(result && result.then){
			if(!into.__proms){
				into.__proms = [];
			}
			into.__proms.push(result.then(v => {
				into[stateField] = v;
			}));
		}else{
			into[stateField] = result;
		}
	}
	
	reset(){
		// Clears any cached state if necessary.
	}
	
	baseRun(){
		var result = this.go();
		
		if(result && result.then){
			return result.then((r) => {
				this.outputs.output = r;
				return r;
			});
		}
		
		// Set output field and return the value:
		this.outputs.output = result;
		return result;
	}
	
	run(){
		return this.baseRun();
	}
}