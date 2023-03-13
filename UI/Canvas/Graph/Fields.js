import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Fields extends Executor {
	
	constructor(props){
		super(props);
	}
	
	async run(field) {
		var obj = await this.state.object.run();
		var res = obj[field];
		this.outputs[field] = res;
		return res;
	}
	
}