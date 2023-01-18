import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class ToList extends Executor {
	
	constructor(){
		super();
		this.state = [];
	}
	
	async go() {
		var output = [];
		output.length = this.state.length;
		
		for(var i=0;i<this.state.length;i++){
			var prop = this.state[i];
			output[i] = prop ? await prop.run() : null;
		}
		
		return output;
	}
	
}