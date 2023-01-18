import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Constant extends Executor {
	
	constructor(props){
		super(props);
		if(props && props.output !== undefined){
			this.state.output = props.output;
		}
	}
	
	loadData(d){
		if(d && d.output){
			// State is as-is here.
			this.state.output = d.output;
		}
	}
	
	async go() {
		return this.state.output;
	}
	
}