import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Component extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this.comp = null;
	}
	
	async run(){
		var result = await this.go();
		
		// Set output and return it:
		this.outputs.output = result;
		return result;
	}
	
	async go() {
		// Wait for each prop to be ready, then output the react component.
		var props = {};
		
		for(var key in this.state){
			var prop = this.state[key];
			var propValue = await prop.run();
			
			if(key == 'componentType'){
				if(!this.comp){
					this.comp = require(propValue).default;
				}
				continue;
			}
			
			props[key] = propValue;
		}
		
		var Component = this.comp;
		return <Component {...props} />;
	}
	
}