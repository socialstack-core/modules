import Executor from './Executor';

export default class NodeOutput extends Executor {
	
	constructor(props){
		super(props);
	}
	
	async go() {
		var {node, field} = this.props;
		await node.run();
		return node.outputs[field];
	}
	
}