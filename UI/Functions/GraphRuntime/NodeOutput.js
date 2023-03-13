import Executor from './Executor';

export default class NodeOutput extends Executor {
	
	constructor(props){
		super(props);
	}
	
	async go() {
		var {node, field} = this.props;
		await node.run(field);
		return node.outputs[field];
	}
	
}