export default class Constant {
	
	constructor(props){
		// super(props); taken out due to no parent class
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
	
	go() {
		return this.state.output;
	}
	
}