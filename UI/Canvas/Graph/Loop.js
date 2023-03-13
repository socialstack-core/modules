import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Loop extends Executor {
	
	constructor(){
		super();
	}
	
	async run(field){
		if(field == 'output'){
			// Being asked for the array. This never caches.
			var arr = await this.go();
			this.outputs.output = arr;
			return arr;
		}else{
			// Being asked for the iteration object.
			return this.outputs[field];
		}
	}
	
	async go() {
		var {listOfItems, loopResult} = this.state;
		
		if(!listOfItems || !loopResult){
			return null;
		}
		
		// Load the items:
		var items = await listOfItems.run();
		
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
			
			// Execute iteration:
			var result = await loopResult.run();
			
			results.push(result);
		}
		
		return results;
	}
	
}