import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Component extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this.comp = null;
	}
	
	go() {
		// Wait for each prop to be ready, then output the react component.
		var props = {};
		
		for(var key in this.state){
			this.readValue(key, props);
		}
		
		return this.onValuesReady(props, () => {
			var { componentType } = props;
			delete props.componentType;
			
			if(!this.comp){
				this.comp = require(componentType).default;
			}
			
			var Component = this.comp;
			return <Component {...props} />;
		});
	}
	
}