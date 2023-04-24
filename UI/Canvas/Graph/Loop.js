import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Loop extends Executor {
	
	constructor(){
		super();
	}
	
	run(field){
		if(field == 'output'){
			// Being asked for the array. This never caches.
			return this.baseRun();
		}else{
			// Being asked for the iteration object.
			return this.outputs[field];
		}
	}
	
	go() {
		var {listOfItems, loopResult} = this.state;
		
		if(!listOfItems || !loopResult){
			return null;
		}
		
		// Load the items:
		var props = {};
		this.readValue('listOfItems', props);
		
		return this.onValuesReady(props, () => {
		
			var results = [];
			
			if(!items){
				return results;
			}
			
			// For each one:
			for(var i=0;i<items.length;i++){
				
				// Set outputs:
				var item = items[i];
				this.outputs.loopItem = item;
				this.outputs.index = i;
				
				// Execute iteration (expected to never be a promise):
				var result = loopResult.run();
				
				results.push(result);
			}
			
			return results;
		});
	}
	
}