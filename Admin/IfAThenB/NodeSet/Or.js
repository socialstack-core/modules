import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';

/*
* Defines the admin UI handler for the Or graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class Or extends GraphNode {
	
	constructor(props){
		super(props);
		this.state = [];
	}
	
	renderFields() {
		
		var fields = [
		];
		
		// this.state is an array. Its length defines how many slots we display.
		var currentSlotCount = this.state.length;
		
		var elementType = 'anything';
		
		if(currentSlotCount){
			elementType = this.getInputType(this.state[0]);
			
			if(!elementType){
				elementType = 'anything';
			}
			
			var outputType = elementType;
			this.setType(elementType);
			
			fields.push({
				key: elementType == 'execute' ? 'thenDo' : 'output',
				name: elementType == 'execute' ? `Then` : `Output`,
				direction: 'out',
				type: outputType,
			});
		}
		
		for(var i=0;i<=currentSlotCount;i++){
			
			var entryNumber = i+1;
			
			fields.push({
				key: i,
				name: `Entry ${entryNumber}`,
				type: elementType
			});
			
		}
		
		return fields;
	}
	
}