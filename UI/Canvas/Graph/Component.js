import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Component extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this.comp = null;
	}
	
	async go() {
		// Wait for each prop to be ready, then output the react component.
		
		if(!this.comp){
			// This graph node caches all the values.
			var props = {};
			
			for(var key in this.state){
				var prop = this.state[key];
				var propValue = await prop.run();
				
				if(key == 'componentType'){
					this.comp = require(propValue).default;
					continue;
				}
				
				props[key] = propValue;
			}
			
			this.constructedProps = props;
		}
		
		var Component = this.comp;
		return <Component {...this.constructedProps} />;
	}
	
}