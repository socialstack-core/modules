export default class NodeOutput {
	
	constructor(props){
		// super(props); Commented out, no parent class
	}
	
	loadData(d){
		// Loads data from the given JSON deserialised object into this executor node.
		// Usually just loads them as a collection of Constant objects. Constant itself retains the original value.
		if(d){
			if(!Constant){
				Constant = require('Admin/CanvasEditor/GraphEditor/Constant').default;
			}
			
			for(var key in d){
				this.state[key] = new Constant({output: d[key]});
			}
		}
	}
	
	go() {
		var {node, field} = this.props;
		var r = node.run(field);
		
		if(r && r.then){
			return r.then(() => node.outputs[field]);
		}
		
		return node.outputs[field];
	}
	
}