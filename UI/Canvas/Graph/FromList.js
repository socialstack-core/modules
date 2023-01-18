import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class FromList extends Executor {
	
	constructor(){
		super();
	}
	
	async go() {
		// Extract an item from a list and return it.
		var list = await this.state.listOfItems.run();
		var index = await this.state.index.run();
		
		if(list && index >=0 && index < list.length){
			return list[index];
		}
		
		return null;
	}
	
}