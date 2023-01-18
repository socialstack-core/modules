import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Loop extends Executor {
	
	constructor(){
		super();
	}
	
	reset(){
		this._ran = false;
	}
	
	async go() {
		var {listOfItems, loopResult} = this.state;
		
		if(!listOfItems || !loopResult){
			return null;
		}
		
		if(this._ran){
			// Occurs when a null is put into the output
			return null;
		}
		 
		this._ran = true;
		
		// Load the items:
		var items = await listOfItems.run();
		
		var results = [];
		
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