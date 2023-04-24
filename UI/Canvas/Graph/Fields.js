import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Fields extends Executor {
	
	constructor(props){
		super(props);
	}
	
	run(field) {
		var obj = this.state.object.run();
		
		var ready = (val) => {
			var res = val[field];
			this.outputs[field] = res;
			return res;
		};
		
		return (obj && obj.then) ? obj.then(ready) : ready(obj);
	}
	
}