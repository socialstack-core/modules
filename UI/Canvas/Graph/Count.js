import Executor from 'UI/Functions/GraphRuntime/Executor';

export default class Count extends Executor {
	
	constructor(){
		super();
	}
	
	go() {
		// Retrieve the number of items in the list.
		var props = {};
		
		this.readValue('listOfItems', props);
		
		return this.onValuesReady(props, () => {
			var { list } = props;

			return list ? list.length : 0;
		});
	}
	
}