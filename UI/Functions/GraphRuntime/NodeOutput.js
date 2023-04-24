import Executor from './Executor';

export default class NodeOutput extends Executor {
	
	constructor(props){
		super(props);
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