import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class FromList extends Executor {
	
	constructor(){
		super();
	}
	
	go() {
		// Extract an item from a list and return it.
		var props = {};
		
		this.readValue('listOfItems', props);
		this.readValue('index', props);
		
		return this.onValuesReady(props, () => {
			var {list,index} = props;
			
			if(list && index >=0 && index < list.length){
				return list[index];
			}
			
			return null;
		});
	}
	
}