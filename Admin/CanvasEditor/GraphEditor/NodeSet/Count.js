import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';

/*
* Defines the admin UI handler for the Count graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Count extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	renderFields() {
		var listOfItems = this.state.listOfItems;
		var inputType = listOfItems ? this.getInputType(listOfItems) : null;
		
		var fields = [{
			key: 'listOfItems',
			name: 'List of items',
			type: inputType || 'array'
		}];

		var outputType = 'number';
		this.setType(outputType);

		fields.push({
			key: 'output',
			type: outputType,
			name: 'output',
			direction: 'out'
		});

		return fields;
	}
	
}